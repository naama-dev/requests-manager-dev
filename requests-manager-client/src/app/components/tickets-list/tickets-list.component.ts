
import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  DestroyRef,
  inject
} from '@angular/core';

import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Subject, of, merge, timer } from 'rxjs';
import { catchError, distinctUntilChanged, finalize, map, switchMap } from 'rxjs/operators';

import { TicketsService } from '../../services/tickets.service';
import { Ticket, TicketListResponse } from '../../models/ticket.model';
import { TicketQuery } from '../../models/ticket-query.model';

import { HttpErrorResponse } from '@angular/common/http';
import { TicketStatus } from '../../models/ticket.model';
import { UpdateTicketStatusRequest } from '../../models/update-ticket-status.model';
import { BulkUpdateStatusResponse, SingleTicketUpdate } from '../../models/bulk-update-status.model';
import { TicketHistoryComponent } from '../ticket-history/ticket-history.component';
@Component({
  selector: 'app-tickets-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TicketHistoryComponent],
  templateUrl: './tickets-list.component.html',
  styleUrl: './tickets-list.component.css'
})
export class TicketsListComponent implements OnInit {
  private readonly ticketsService = inject(TicketsService);
  private readonly destroyRef = inject(DestroyRef);

  // כל בקשות השליפה עוברות דרך הזרם הזה.
  private readonly queryChanges = new Subject<TicketQuery>();
  private readonly searchChanges = new Subject<TicketQuery>();
  tickets: Ticket[] = [];
  response: TicketListResponse | null = null;

  loading = false;
  errorMessage = '';

  query: TicketQuery = {
    page: 1,
    pageSize: 20,
    sortBy: 'CreatedAt',
    sortDirection: 'Desc'
  };

  searchControl = new FormControl('', { nonNullable: true });
  statusControl = new FormControl('');
  priorityControl = new FormControl('');
  organizationNameControl = new FormControl('');
  assignedToControl = new FormControl('');

  sortByControl = new FormControl('CreatedAt', {
    nonNullable: true
  });

  sortDirectionControl = new FormControl<'Asc' | 'Desc'>('Desc', {
    nonNullable: true
  });

  fromDateControl = new FormControl('');
  toDateControl = new FormControl('');


  selectedStatuses: Record<number, TicketStatus> = {};
  updatingTicketIds = new Set<number>();
  statusMessages: Record<number, string> = {};
  // מזהי הפניות שנבחרו לעדכון מרוכז
  selectedTickets = new Map<number, Ticket>();

  get selectedTicketIds(): Set<number> {
    return new Set(this.selectedTickets.keys());
  }
  // הסטטוס שאליו נרצה לעדכן את הפניות
  bulkStatusControl = new FormControl<TicketStatus>(
    'InProgress',
    { nonNullable: true }
  );

  bulkUpdating = false;
  bulkUpdateMessage = '';
  bulkUpdateResult: BulkUpdateStatusResponse | null = null;
  expandedHistoryTicketId: number | null = null;
  first100Selected: boolean = false;
  private latestRequestId = 0;

  ngOnInit(): void {
    // כל בקשות השליפה עוברות דרך זרם מרכזי אחד.
    merge(
      this.queryChanges.pipe(
        map(query => ({
          query,
          debounce: false
        }))
      ),
      this.searchChanges.pipe(
        map(query => ({
          query,
          debounce: true
        }))
      )
    ).pipe(
      switchMap(({ query, debounce }) => {
        const requestId = ++this.latestRequestId;

        this.loading = true;
        this.errorMessage = '';

        const request$ = debounce
          ? timer(500).pipe(
            switchMap(() =>
              this.ticketsService.getTickets({ ...query })
            )
          )
          : this.ticketsService.getTickets({ ...query });

        return request$.pipe(
          map(result => ({
            result,
            requestId,
            error: false
          })),
          catchError((error: HttpErrorResponse) => {
            console.error('Failed to load tickets:', error);

            return of({
              result: null,
              requestId,
              error: true
            });
          }),
          finalize(() => {
            // בקשה ישנה לא תכבה את הטעינה של בקשה חדשה.
            if (requestId === this.latestRequestId) {
              this.loading = false;
            }
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(({ result, requestId, error }) => {
      // מתעלמים מתוצאות שאינן שייכות לבקשה האחרונה.
      if (requestId !== this.latestRequestId) {
        return;
      }

      if (error || result === null) {
        this.errorMessage = 'טעינת הפניות נכשלה. נסי שוב.';
        return;
      }

      this.errorMessage = '';
      this.response = result;
      this.tickets = result.items;

      // סנכרון שדות המיון עם השאילתה הנוכחית.
      this.sortByControl.setValue(this.query.sortBy, {
        emitEvent: false
      });

      this.sortDirectionControl.setValue(
        this.query.sortDirection,
        { emitEvent: false }
      );
    });

    // חיפוש חופשי לפי כותרת או ארגון.
    this.searchControl.valueChanges.pipe(
      map(value => value.trim()),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(search => {
      this.query = {
        ...this.query,
        search: search || undefined,
        page: 1
      };

      this.scheduleSearch();
    });

    // חיפוש לפי שם ארגון.
    this.organizationNameControl.valueChanges.pipe(
      map(value => (value ?? '').trim()),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(organizationName => {
      this.query = {
        ...this.query,
        organizationName: organizationName || undefined,
        page: 1
      };

      this.scheduleSearch();
    });

    // חיפוש לפי אחראי.
    this.assignedToControl.valueChanges.pipe(
      map(value => (value ?? '').trim()),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(assignedTo => {
      this.query = {
        ...this.query,
        assignedTo: assignedTo || undefined,
        page: 1
      };

      this.scheduleSearch();
    });

    // פילטרים בחירה ותאריכים — הפעלה מיידית.
    merge(
      this.statusControl.valueChanges,
      this.priorityControl.valueChanges,
      this.fromDateControl.valueChanges,
      this.toDateControl.valueChanges
    ).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      this.applyAutomaticFilters();
    });

    // טעינה ראשונית.
    this.loadTickets();
  }
  private scheduleSearch(): void {
    this.searchChanges.next({ ...this.query });
  }

  loadTickets(): void {
    this.queryChanges.next({ ...this.query });
  }
  private applyAutomaticFilters(): void {
    this.query = {
      ...this.query,
      status: this.statusControl.value || undefined,
      priority: this.priorityControl.value || undefined,
      fromDate: this.fromDateControl.value || undefined,
      toDate: this.toDateControl.value || undefined,
      page: 1
    };

    this.loadTickets();
  }

  sortByColumn(column: string): void {
    const direction: 'Asc' | 'Desc' =
      this.query.sortBy === column &&
        this.query.sortDirection === 'Asc'
        ? 'Desc'
        : 'Asc';

    this.query = {
      ...this.query,
      sortBy: column,
      sortDirection: direction,
      page: 1
    };

    this.loadTickets();
  }

  getSortIcon(column: string): string {
    if (this.query.sortBy !== column) {
      return '↕';
    }

    return this.query.sortDirection === 'Asc' ? '↑' : '↓';
  }

  clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.statusControl.setValue('', { emitEvent: false });
    this.priorityControl.setValue('', { emitEvent: false });
    this.organizationNameControl.setValue('', { emitEvent: false });
    this.assignedToControl.setValue('', { emitEvent: false });
    this.fromDateControl.setValue('', { emitEvent: false });
    this.toDateControl.setValue('', { emitEvent: false });

    this.query = {
      page: 1,
      pageSize: this.query.pageSize,
      sortBy: 'CreatedAt',
      sortDirection: 'Desc'
    };

    this.sortByControl.setValue('CreatedAt', { emitEvent: false });
    this.sortDirectionControl.setValue('Desc', { emitEvent: false });

    this.loadTickets();
  }

  previousPage(): void {
    if (this.loading || this.query.page <= 1) {
      return;
    }

    this.query = {
      ...this.query,
      page: this.query.page - 1
    };

    this.loadTickets();
  }

  nextPage(): void {
    if (
      this.loading ||
      !this.response ||
      this.query.page >= this.response.totalPages
    ) {
      return;
    }

    this.query = {
      ...this.query,
      page: this.query.page + 1
    };

    this.loadTickets();
  }

  retry(): void {
    this.loadTickets();
  }

  getSelectedStatus(
    ticketId: number,
    currentStatus: TicketStatus
  ): TicketStatus {
    return this.selectedStatuses[ticketId] ?? currentStatus;
  }

  onStatusSelectionChange(
    ticketId: number,
    status: TicketStatus
  ): void {
    this.selectedStatuses = {
      ...this.selectedStatuses,
      [ticketId]: status
    };

    this.statusMessages = {
      ...this.statusMessages,
      [ticketId]: ''
    };
  }

  isUpdating(ticketId: number): boolean {
    return this.updatingTicketIds.has(ticketId);
  }

  saveStatus(ticket: Ticket): void {
    const status = this.getSelectedStatus(ticket.id, ticket.status);

    if (status === ticket.status || this.isUpdating(ticket.id)) {
      return;
    }

    this.updatingTicketIds.add(ticket.id);

    const request: UpdateTicketStatusRequest = {
      status,
      rowVersion: ticket.rowVersion
    };

    this.ticketsService.updateTicketStatus(ticket.id, request).subscribe({
      next: updatedTicket => {
        this.tickets = this.tickets.map(item =>
          item.id === updatedTicket.id ? updatedTicket : item
        );

        this.selectedStatuses = {
          ...this.selectedStatuses,
          [ticket.id]: updatedTicket.status
        };

        this.statusMessages = {
          ...this.statusMessages,
          [ticket.id]: 'הסטטוס עודכן בהצלחה.'
        };

        this.updatingTicketIds.delete(ticket.id);

        // מרעננים גם את הסיכומים והנתונים מהשרת.
        this.loadTickets();
      },

      error: (error: HttpErrorResponse) => {
        this.updatingTicketIds.delete(ticket.id);

        if (error.status === 409) {
          this.statusMessages = {
            ...this.statusMessages,
            [ticket.id]:
              'הפנייה עודכנה על ידי משתמש אחר. הנתונים מרועננים; בדקי את הסטטוס החדש ונסי שוב.'
          };
          this.loadTickets();
          return;
        }

        if (error.status === 404) {
          this.statusMessages = {
            ...this.statusMessages,
            [ticket.id]: 'הפנייה לא נמצאה. מרעננים את הרשימה.'
          };

          this.loadTickets();
          return;
        }

        this.statusMessages = {
          ...this.statusMessages,
          [ticket.id]: error.error?.message ??
            'עדכון הסטטוס נכשל. נסי שוב.'
        };
      }
    });
  }
  clearStatusMessage(ticketId: number): void {
    delete this.statusMessages[ticketId];
  }
  isTicketSelected(ticketId: number): boolean {
    return this.selectedTicketIds.has(ticketId);
  }


  toggleTicketSelection(ticket: Ticket, checked: boolean): void {
    const updated = new Map(this.selectedTickets);

    if (checked) {
      updated.set(ticket.id, ticket);
    } else {
      updated.delete(ticket.id);
    }

    this.selectedTickets = updated;
  }

  toggleCurrentPage(checked: boolean): void {
    const updated = new Map(this.selectedTickets);

    for (const ticket of this.tickets) {
      if (checked) {
        updated.set(ticket.id, ticket);
      } else {
        // מסירים רק את הבחירות בעמוד הנוכחי
        updated.delete(ticket.id);
      }
    }

    this.selectedTickets = updated;
  }

  get allCurrentPageSelected(): boolean {
    return this.tickets.length > 0 &&
      this.tickets.every(ticket =>
        this.selectedTickets.has(ticket.id)
      );
  }

  bulkUpdateStatus(): void {
    if (this.bulkUpdating || this.selectedTicketIds.size === 0) {
      return;
    }

    const targetStatus = this.bulkStatusControl.value;

    const updates: SingleTicketUpdate[] =
      Array.from(this.selectedTickets.values()).map(ticket => ({
        id: ticket.id,
        status: targetStatus,
        rowVersion: ticket.rowVersion
      }));

    if (updates.length === 0) {
      this.bulkUpdateMessage =
        'לא נמצאו פניות נבחרות בעמוד הנוכחי.';
      return;
    }

    if (updates.length > 100) {
      this.bulkUpdateMessage =
        'ניתן לעדכן עד 100 פניות בכל פעולה.';
      return;
    }

    this.bulkUpdating = true;
    this.bulkUpdateMessage = '';
    this.bulkUpdateResult = null;

    this.ticketsService.bulkUpdateStatus({ updates }).subscribe({
      next: result => {
        this.bulkUpdateResult = result;

        this.bulkUpdateMessage =
          `העדכון הסתיים: ${result.successCount} הצליחו, ` +
          `${result.failureCount} נכשלו.`;

        // מסירים מהבחירה רק פניות שעודכנו בהצלחה.
        const updated = new Map(this.selectedTickets);

        for (const item of result.results) {
          if (item.isSuccess) {
            updated.delete(item.id);
          }
        }

        this.selectedTickets = updated;
        this.bulkUpdating = false;

        // טעינת הרשימה מחדש כדי לקבל סטטוסים ו-RowVersion עדכניים.
        this.loadTickets();
      },

      error: error => {
        this.bulkUpdating = false;

        this.bulkUpdateMessage =
          error.error?.message ??
          'העדכון המרוכז נכשל. נסי שוב.';
      }
    });
  }
  translateStatusMessage(message: string): string {
    const translations: { [key: string]: string } = {
      'Concurrency conflict: The ticket was modified by another user.':
        'העדכון נכשל: משתמש אחר שינה את הפנייה. יש לרענן את הנתונים ולנסות שוב.',

      'Updated successfully.':
        'הפנייה עודכנה בהצלחה.',

      'Updated successfully':
        'הפנייה עודכנה בהצלחה.'
    };

    return translations[message] ?? message;
  }
  toggleTicketHistory(ticketId: number): void {
    this.expandedHistoryTicketId =
      this.expandedHistoryTicketId === ticketId
        ? null
        : ticketId;
  }


  onSelectFirst100Change(checked: boolean): void {
    if (!checked) {
      this.selectedTickets = new Map<number, Ticket>();
      this.first100Selected = false;
      this.bulkUpdateMessage = '';
      return;
    }

    this.selectFirst100Tickets();
  }

  selectFirst100Tickets(): void {
    if (this.bulkUpdating || this.loading) {
      return;
    }

    const query: TicketQuery = {
      ...this.query,
      page: 1,
      pageSize: 100
    };

    this.ticketsService.getTickets(query).subscribe({
      next: response => {
        const selected = new Map<number, Ticket>();

        for (const ticket of response.items.slice(0, 100)) {
          selected.set(ticket.id, ticket);
        }

        this.selectedTickets = selected;
        this.first100Selected = selected.size > 0;

        this.bulkUpdateMessage =
          `נבחרו ${selected.size} פניות לעדכון.`;
        this.bulkUpdateResult = null;
      },
      error: error => {
        this.bulkUpdateMessage =
          error.error?.message ??
          'טעינת הפניות לבחירה נכשלה.';
      }
    });
  }
  clearSelection(): void {
    this.selectedTickets = new Map<number, Ticket>();
    this.first100Selected = false;
    this.bulkUpdateMessage = '';
    this.bulkUpdateResult = null;
  }

}

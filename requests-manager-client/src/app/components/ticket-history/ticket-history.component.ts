
import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges } from '@angular/core';
import { TicketsService } from '../../services/tickets.service';
import { TicketHistory } from '../../models/ticket-history.model';

@Component({
  selector: 'app-ticket-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ticket-history.component.html',
  styleUrl: './ticket-history.component.css'
})
export class TicketHistoryComponent implements OnChanges {
  @Input() ticketId!: number;

  history: TicketHistory[] = [];
  loading = false;
  errorMessage = '';

  constructor(private ticketsService: TicketsService) {}

  ngOnChanges(): void {
    if (this.ticketId == null) {
      return;
    }

    this.loadHistory();
  }

  loadHistory(): void {
    this.loading = true;
    this.errorMessage = '';
    this.history = [];

    this.ticketsService.getTicketHistory(this.ticketId).subscribe({
      next: (history) => {
        this.history = history;
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;

        if (error.status === 404) {
          this.errorMessage = 'הפנייה לא נמצאה.';
        } else {
          this.errorMessage = 'אירעה שגיאה בטעינת היסטוריית הפנייה.';
        }
      }
    });
  }

  translateStatus(status: string | null | undefined): string {
    const statuses: Record<string, string> = {
      New: 'חדשה',
      InProgress: 'בטיפול',
      Completed: 'הושלמה',
      Waiting: 'ממתינה'
    };

    return status ? statuses[status] ?? status : '—';
  }
}

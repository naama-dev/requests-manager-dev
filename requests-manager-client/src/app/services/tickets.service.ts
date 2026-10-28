
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { TicketListResponse } from '../models/ticket.model';
import { TicketQuery } from '../models/ticket-query.model';
import { Ticket } from '../models/ticket.model';
import { UpdateTicketStatusRequest } from '../models/update-ticket-status.model';
import { BulkUpdateStatusRequest, BulkUpdateStatusResponse } from '../models/bulk-update-status.model';
import { TicketHistory } from '../models/ticket-history.model';
@Injectable({
  providedIn: 'root'
})
export class TicketsService {
  private readonly http = inject(HttpClient);

  // נחליף לכתובת ה-API האמיתית שלך
  private readonly apiUrl = 'https://localhost:7040/api/tickets';

  getTickets(query: TicketQuery): Observable<TicketListResponse> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.priority) {
      params = params.set('priority', query.priority);
    }

    if (query.organizationName) {
      params = params.set('organizationName', query.organizationName);
    }

    if (query.assignedTo) {
      params = params.set('assignedTo', query.assignedTo);
    }

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.fromDate) {
      params = params.set('fromDate', query.fromDate);
    }

    if (query.toDate) {
      params = params.set('toDate', query.toDate);
    }

    return this.http.get<TicketListResponse>(
      this.apiUrl,
      { params }
    );
  }

updateTicketStatus(
  id: number,
  request: UpdateTicketStatusRequest
): Observable<Ticket> {
  return this.http.put<Ticket>(
    `${this.apiUrl}/${id}/status`,
    request
  );
}
bulkUpdateStatus(
  request: BulkUpdateStatusRequest
) {
  return this.http.put<BulkUpdateStatusResponse>(
    `${this.apiUrl}/bulk-status`,
    request
  );
}
getTicketHistory(id: number): Observable<TicketHistory[]> {
  return this.http.get<TicketHistory[]>(`${this.apiUrl}/${id}/history`);
}
}

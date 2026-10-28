
import { TicketStatus } from './ticket.model';

export interface UpdateTicketStatusRequest {
  status: TicketStatus;
  rowVersion: string;
}

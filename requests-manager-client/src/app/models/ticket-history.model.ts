export interface TicketHistory {
  id: number;
  ticketId: number;
  previousStatus: string;
  newStatus: string;
  changedBy: string;
  changedAt: string;
}
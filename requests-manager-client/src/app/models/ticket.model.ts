
export type TicketStatus =
  | 'New'
  | 'Waiting'
  | 'InProgress'
  | 'Completed';

export type TicketPriority =
  | 'Low'
  | 'Medium'
  | 'High';

export interface Ticket {
  id: number;
  title: string;
  organizationName: string;
  status: TicketStatus;
  priority: TicketPriority;
  assignedTo: string;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface TicketListResponse {
  items: Ticket[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  statusCounts: Record<string, number>;
  priorityCounts: Record<string, number>;
}

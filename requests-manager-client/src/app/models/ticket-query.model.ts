
export interface TicketQuery {
  page: number;
  pageSize: number;
  status?: string;
  priority?: string;
  organizationName?: string;
  assignedTo?: string;
  search?: string;
  fromDate?: string;
  toDate?: string;
  sortBy: string;
  sortDirection: 'Asc' | 'Desc';
}

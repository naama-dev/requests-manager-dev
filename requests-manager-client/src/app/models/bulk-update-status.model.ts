
export interface SingleTicketUpdate {
  id: number;
  status: string;
  rowVersion: string;
}

export interface BulkUpdateStatusRequest {
  updates: SingleTicketUpdate[];
}

export interface BulkUpdateItemResult {
  id: number;
  isSuccess: boolean;
  message: string;
}

export interface BulkUpdateStatusResponse {
  successCount: number;
  failureCount: number;
  results: BulkUpdateItemResult[];
}

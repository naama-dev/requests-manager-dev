namespace RequestsManager.Api.DTOs
{
    public class BulkUpdateStatusDto
    {
        public List<SingleTicketUpdateDto> Updates { get; set; } = new();
    }


    public class SingleTicketUpdateDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RowVersion { get; set; } = string.Empty; // מחרוזת Base64 עבור ה-RowVersion
    }

    public class BulkUpdateResponseDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkUpdateItemResultDto> Results { get; set; } = new();
    }

    public class BulkUpdateItemResultDto
    {
        public int Id { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

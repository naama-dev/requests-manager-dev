namespace RequestsManager.Api.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}
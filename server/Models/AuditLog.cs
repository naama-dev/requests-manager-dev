namespace RequestsManager.Api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string ChangedBy { get; set; } = "SystemUser";
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // קשר ניווט אופציונלי לטבלת הפניות
        public Ticket? Ticket { get; set; }
    }
}
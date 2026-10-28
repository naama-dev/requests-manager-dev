
using System.ComponentModel.DataAnnotations;

namespace RequestsManager.Api.Parameters
{
    public class TicketQueryParameters
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? OrganizationName { get; set; }
        public string? AssignedTo { get; set; }

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "Desc";
    }
}

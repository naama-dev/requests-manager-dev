namespace RequestsManager.Api.DTOs
{
    public class TicketListResponseDto
    {
        public List<TicketDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public Dictionary<string, int> StatusCounts { get; set; } = new();

        public Dictionary<string, int> PriorityCounts { get; set; } = new();
    }
}
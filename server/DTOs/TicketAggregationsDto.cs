namespace RequestsManager.Api.DTOs
{
    public class TicketAggregationsDto
    {
        public Dictionary<string, int> StatusCounts { get; set; }
            = new();

        public Dictionary<string, int> PriorityCounts { get; set; }
            = new();

        public int TotalCount { get; set; }
    }
}
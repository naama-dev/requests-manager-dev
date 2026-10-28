
namespace RequestsManager.Api.Validators
{
    public static class TicketValidation
    {
        public static readonly string[] AllowedStatuses =
        {
            "New",
            "InProgress",
            "Waiting",
            "Completed"
        };

        public static readonly string[] AllowedPriorities =
        {
            "Low",
            "Medium",
            "High"
        };
    }
}

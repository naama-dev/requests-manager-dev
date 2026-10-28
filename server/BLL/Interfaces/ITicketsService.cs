using RequestsManager.Api.DTOs;
using RequestsManager.Api.Parameters;

namespace RequestsManager.Api.BLL.Interfaces
{
    public interface ITicketsService
    {
        Task<TicketListResponseDto> GetTicketsAsync(TicketQueryParameters parameters, CancellationToken cancellationToken);
        Task<TicketAggregationsDto> GetAggregationsAsync(CancellationToken cancellationToken);
        Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateTicketStatusDto dto, CancellationToken cancellationToken);
        Task<IEnumerable<AuditLogDto>> GetTicketHistoryAsync(int ticketId, CancellationToken cancellationToken);
        Task<BulkUpdateResponseDto> BulkUpdateStatusAsync(BulkUpdateStatusDto dto, CancellationToken cancellationToken);
    }
}
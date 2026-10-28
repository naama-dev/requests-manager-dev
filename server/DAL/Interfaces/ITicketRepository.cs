using RequestsManager.Api.Models;

namespace RequestsManager.Api.DAL.Interfaces
{
    public interface ITicketRepository
    {
        IQueryable<Ticket> GetTicketsQuery();
        Task<Ticket?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);
        void SetOriginalRowVersion(Ticket ticket, byte[] rowVersion); 
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<IEnumerable<AuditLog>> GetAuditLogsForTicketAsync(int ticketId, CancellationToken cancellationToken);
        Task<bool> TicketExistsAsync(int ticketId, CancellationToken cancellationToken);
        Task<List<Ticket>> GetByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
        void DiscardFailedUpdate(Ticket ticket);

    }
}
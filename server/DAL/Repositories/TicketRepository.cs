using Microsoft.EntityFrameworkCore;
using RequestsManager.Api.Data;
using RequestsManager.Api.DAL.Interfaces;
using RequestsManager.Api.Models;

namespace RequestsManager.Api.DAL.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly AppDbContext _context;

        public TicketRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Ticket> GetTicketsQuery()
        {
            return _context.Tickets.AsNoTracking();
        }

        public async Task<Ticket?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        public void SetOriginalRowVersion(Ticket ticket, byte[] rowVersion)
        {
            _context.Entry(ticket).Property(t => t.RowVersion).OriginalValue = rowVersion;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            var changedTickets = _context.ChangeTracker
                .Entries<Ticket>()
                .Where(e =>
                    e.State == EntityState.Added ||
                    (e.State == EntityState.Modified &&
                     e.Property(t => t.Status).IsModified &&
                     e.Property(t => t.Status).OriginalValue !=
                     e.Property(t => t.Status).CurrentValue))
                .ToList();

            var changedAt = DateTime.UtcNow;

            foreach (var entry in changedTickets)
            {
                if (entry.State == EntityState.Added)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        PreviousStatus = "None",
                        NewStatus = entry.Entity.Status,
                        ChangedBy = "SystemUser",
                        ChangedAt = changedAt,
                        Ticket = entry.Entity
                    });
                }
                else
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        TicketId = entry.Entity.Id,
                        PreviousStatus =
                            entry.Property(t => t.Status).OriginalValue,
                        NewStatus =
                            entry.Property(t => t.Status).CurrentValue,
                        ChangedBy = "SystemUser",
                        ChangedAt = changedAt
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsForTicketAsync(int ticketId, CancellationToken cancellationToken)
        {
            return await _context.AuditLogs
                .Where(log => log.TicketId == ticketId)
                .OrderByDescending(log => log.ChangedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> TicketExistsAsync(int ticketId, CancellationToken cancellationToken)
        {
            return await _context.Tickets
                .AnyAsync(t => t.Id == ticketId, cancellationToken);
        }
        public async Task<List<Ticket>> GetByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
        {
            return await _context.Tickets
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken);
        }
        public void DiscardFailedUpdate(Ticket ticket)
        {
            var ticketEntry = _context.Entry(ticket);

            ticketEntry.State = EntityState.Unchanged;

            var pendingAuditLogs = _context.ChangeTracker
                .Entries<AuditLog>()
                .Where(e =>
                    e.State == EntityState.Added &&
                    (e.Entity.TicketId == ticket.Id ||
                     e.Entity.Ticket == ticket))
                .ToList();

            foreach (var auditEntry in pendingAuditLogs)
            {
                auditEntry.State = EntityState.Detached;
            }
        }
    }
}
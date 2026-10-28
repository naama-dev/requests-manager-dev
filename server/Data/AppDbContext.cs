
using Microsoft.EntityFrameworkCore;
using RequestsManager.Api.Models;

namespace RequestsManager.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var ticket = modelBuilder.Entity<Ticket>();

            if (Database.IsSqlite())
            {
                ticket.Property(t => t.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedNever();
            }
            else
            {
                ticket.Property(t => t.RowVersion)
                    .IsRowVersion();
            }

            ticket.HasIndex(t => new { t.CreatedAt, t.Id })
                .IsDescending(true, false)
                .HasDatabaseName("IX_Tickets_CreatedAt_Id");

            ticket.HasIndex(t => new
            {
                t.Status,
                t.Priority,
                t.CreatedAt
            })
                .HasDatabaseName("IX_Tickets_Status_Priority_CreatedAt");

            ticket.HasIndex(t => new { t.AssignedTo, t.CreatedAt })
                .HasDatabaseName("IX_Tickets_AssignedTo_CreatedAt");

            ticket.HasIndex(t => new { t.Title, t.Id })
                .HasDatabaseName("IX_Tickets_Title_Id");

            ticket.HasIndex(t => new { t.OrganizationName, t.Id })
                .HasDatabaseName("IX_Tickets_OrganizationName_Id");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.TicketId, a.ChangedAt })
                .HasDatabaseName("IX_AuditLogs_TicketId_ChangedAt");
        }
    }
}

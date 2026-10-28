
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RequestsManager.Api.Data;
using RequestsManager.Api.DTOs;
using RequestsManager.Api.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RequestsManager.Tests;

public class TicketsApiIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<AppDbContext>();

                    services.AddSingleton<SqliteRowVersionInterceptor>();

                    services.AddDbContext<AppDbContext>((provider, options) =>
                    {
                        options.UseSqlite(connection);
                        options.AddInterceptors(
                            provider.GetRequiredService<SqliteRowVersionInterceptor>());
                    });
                });
            });
    }

    private static async Task<(
        Microsoft.Data.Sqlite.SqliteConnection Connection,
        WebApplicationFactory<Program> Factory,
        int TicketId,
        byte[] RowVersion)> CreateTestEnvironmentAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var factory = CreateFactory(connection);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var ticket = new Ticket
        {
            Title = "Integration test ticket",
            OrganizationName = "Test organization",
            Status = "New",
            Priority = "Medium",
            AssignedTo = "Tester",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return (connection, factory, ticket.Id, ticket.RowVersion.ToArray());
    }

    [Fact]
    public async Task GetTickets_ReturnsSuccess()
    {
        var env = await CreateTestEnvironmentAsync();

        try
        {
            using var client = env.Factory.CreateClient();

            var response = await client.GetAsync("/api/Tickets");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await env.Factory.DisposeAsync();
            await env.Connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateTicketStatus_SavesStatusToDatabase()
    {
        var env = await CreateTestEnvironmentAsync();

        try
        {
            using var client = env.Factory.CreateClient();

            var response = await client.PutAsJsonAsync(
                $"/api/Tickets/{env.TicketId}/status",
                new UpdateTicketStatusDto
                {
                    Status = "InProgress",
                    RowVersion = env.RowVersion
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = env.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var saved = await db.Tickets
                .AsNoTracking()
                .SingleAsync(t => t.Id == env.TicketId);

            Assert.Equal("InProgress", saved.Status);
        }
        finally
        {
            await env.Factory.DisposeAsync();
            await env.Connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateTicketStatus_InvalidStatus_ReturnsBadRequest()
    {
        var env = await CreateTestEnvironmentAsync();

        try
        {
            using var client = env.Factory.CreateClient();

            var response = await client.PutAsJsonAsync(
                $"/api/Tickets/{env.TicketId}/status",
                new UpdateTicketStatusDto
                {
                    Status = "InvalidStatus",
                    RowVersion = env.RowVersion
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await env.Factory.DisposeAsync();
            await env.Connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateTicketStatus_StaleRowVersion_ReturnsConflict()
    {
        var env = await CreateTestEnvironmentAsync();

        try
        {
            using var client = env.Factory.CreateClient();

            var firstResponse = await client.PutAsJsonAsync(
                $"/api/Tickets/{env.TicketId}/status",
                new UpdateTicketStatusDto
                {
                    Status = "InProgress",
                    RowVersion = env.RowVersion
                });

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            var secondResponse = await client.PutAsJsonAsync(
                $"/api/Tickets/{env.TicketId}/status",
                new UpdateTicketStatusDto
                {
                    Status = "Completed",
                    RowVersion = env.RowVersion
                });

            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }
        finally
        {
            await env.Factory.DisposeAsync();
            await env.Connection.DisposeAsync();
        }
    }
}

public sealed class SqliteRowVersionInterceptor : SaveChangesInterceptor
{
    private static long _version;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetRowVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetRowVersions(eventData.Context);
        return base.SavingChangesAsync(
            eventData, result, cancellationToken);
    }

    private static void SetRowVersions(DbContext? context)
    {
        if (context == null)
            return;

        foreach (var entry in context.ChangeTracker
                     .Entries<Ticket>()
                     .Where(e => e.State == EntityState.Added ||
                                 e.State == EntityState.Modified))
        {
            entry.Property(t => t.RowVersion).CurrentValue =
                BitConverter.GetBytes(
                    Interlocked.Increment(ref _version));
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RequestsManager.Api.BLL.Interfaces;
using RequestsManager.Api.DAL.Interfaces;
using RequestsManager.Api.DTOs;
using RequestsManager.Api.Models;
using RequestsManager.Api.Parameters;
using RequestsManager.Api.Validators;
namespace RequestsManager.Api.BLL.Services 
{
    public class TicketsService : ITicketsService
    {
        private readonly ITicketRepository _ticketRepository; 
        private readonly IMemoryCache _cache;

        private const string AggregationsCacheKey = "Tickets_Aggregations";
        private static readonly TimeSpan AggregationsCacheDuration =
            TimeSpan.FromSeconds(60);
        public TicketsService(ITicketRepository ticketRepository, IMemoryCache cache)
        {
            _ticketRepository = ticketRepository;
            _cache = cache;
        }



        public async Task<TicketListResponseDto> GetTicketsAsync(
            TicketQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            IQueryable<Ticket> query = _ticketRepository.GetTicketsQuery();

            if (!string.IsNullOrWhiteSpace(parameters.Status))
            {
                query = query.Where(t => t.Status == parameters.Status);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Priority))
            {
                query = query.Where(t => t.Priority == parameters.Priority);
            }

            if (!string.IsNullOrWhiteSpace(parameters.AssignedTo))
            {
                query = query.Where(t => t.AssignedTo == parameters.AssignedTo);
            }

            if (parameters.FromDate.HasValue)
            {
                query = query.Where(
                    t => t.CreatedAt >= parameters.FromDate.Value);
            }

            if (parameters.ToDate.HasValue)
            {
                var toDateExclusive = parameters.ToDate.Value.Date.AddDays(1);

                query = query.Where(
                    t => t.CreatedAt < toDateExclusive);
            }

            if (!string.IsNullOrWhiteSpace(parameters.OrganizationName))
            {
                var organizationName = parameters.OrganizationName.Trim();

                query = query.Where(
                    t => t.OrganizationName.Contains(organizationName));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var search = parameters.Search.Trim();

                query = query.Where(t =>
                    t.Title.Contains(search) ||
                    t.OrganizationName.Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(
                totalCount / (double)parameters.PageSize);

            List<TicketDto> items;

            if (parameters.Page > totalPages)
            {
                items = new List<TicketDto>();
            }
            else
            {
                var sortedQuery = ApplySorting(
                    query,
                    parameters.SortBy,
                    parameters.SortDirection);
                items = await sortedQuery
                    .Skip((parameters.Page - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .Select(t => new TicketDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        OrganizationName = t.OrganizationName,
                        Status = t.Status,
                        Priority = t.Priority,
                        AssignedTo = t.AssignedTo,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        RowVersion = t.RowVersion
                    })

                    .ToListAsync(cancellationToken);
            }

            var aggregations = await GetAggregationsAsync(cancellationToken);

            return new TicketListResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalPages = totalPages,
                StatusCounts = aggregations.StatusCounts,
                PriorityCounts = aggregations.PriorityCounts
            };
        }

        private static IQueryable<Ticket> ApplySorting(
            IQueryable<Ticket> query,
            string? sortBy,
            string? sortDirection)
        {
            var descending = string.Equals(
                sortDirection,
                "Desc",
                StringComparison.OrdinalIgnoreCase);

            return (sortBy?.Trim().ToLowerInvariant()) switch
            {
                "title" => descending
                    ? query.OrderByDescending(t => t.Title).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.Title).ThenBy(t => t.Id),

                "status" => descending
                    ? query.OrderByDescending(t => t.Status).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.Status).ThenBy(t => t.Id),

                "priority" => descending
                    ? query.OrderByDescending(t => t.Priority).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.Priority).ThenBy(t => t.Id),

                "organizationname" => descending
                    ? query.OrderByDescending(t => t.OrganizationName).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.OrganizationName).ThenBy(t => t.Id),

                "updatedat" => descending
                    ? query.OrderByDescending(t => t.UpdatedAt).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.UpdatedAt).ThenBy(t => t.Id),

                "assignedto" => descending
                    ? query.OrderByDescending(t => t.AssignedTo).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.AssignedTo).ThenBy(t => t.Id),

                "createdat" => descending
                    ? query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
                    : query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),

                _ => query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
            };
        }


        public async Task<TicketAggregationsDto> GetAggregationsAsync(
    CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(
                AggregationsCacheKey,
                out TicketAggregationsDto? cachedAggregations)
                && cachedAggregations != null)
            {
                return cachedAggregations;
            }

            var query = _ticketRepository.GetTicketsQuery();

            var totalCount = await query.CountAsync(cancellationToken);

            var statusCounts = await query
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    x => x.Status,
                    x => x.Count,
                    cancellationToken);

            var priorityCounts = await query
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    x => x.Priority,
                    x => x.Count,
                    cancellationToken);

            var aggregations = new TicketAggregationsDto
            {
                TotalCount = totalCount,
                StatusCounts = statusCounts,
                PriorityCounts = priorityCounts
            };

            _cache.Set(
                AggregationsCacheKey,
                aggregations,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AggregationsCacheDuration
                });

            return aggregations;
        }
        public async Task<TicketDto?> UpdateTicketStatusAsync(
    int id,
    UpdateTicketStatusDto dto,
    CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(dto.Status) ||
                !TicketValidation.AllowedStatuses.Contains(dto.Status.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Invalid status value. Allowed values: New, Waiting, InProgress, Completed.");
            }

            dto.Status = dto.Status.Trim();
            var ticket = await _ticketRepository.GetByIdForUpdateAsync(id, cancellationToken);
            if (ticket == null)
            {
                return null; 
            }

            if (dto.RowVersion == null || dto.RowVersion.Length == 0)
            {
                throw new ArgumentException("RowVersion is required.");
            }

            _ticketRepository.SetOriginalRowVersion(ticket, dto.RowVersion);

            ticket.Status = dto.Status;
            ticket.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _ticketRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Concurrency conflict: The ticket was modified or updated by another user.");
            }
            _cache.Remove(AggregationsCacheKey);

            return new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                OrganizationName = ticket.OrganizationName,
                Status = ticket.Status,
                Priority = ticket.Priority,
                AssignedTo = ticket.AssignedTo,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                RowVersion = ticket.RowVersion
            };
        }
        public async Task<IEnumerable<AuditLogDto>> GetTicketHistoryAsync(int ticketId, CancellationToken cancellationToken)
        {
            var logs = await _ticketRepository.GetAuditLogsForTicketAsync(ticketId, cancellationToken);

            if (!logs.Any())
            {
                var exists = await _ticketRepository.TicketExistsAsync(ticketId, cancellationToken);
                if (!exists)
                {
                    throw new KeyNotFoundException($"Ticket with ID {ticketId} was not found.");
                }
            }

            return logs.Select(log => new AuditLogDto
            {
                Id = log.Id,
                TicketId = log.TicketId,
                PreviousStatus = log.PreviousStatus,
                NewStatus = log.NewStatus,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt
            });
        }
        public async Task<BulkUpdateResponseDto> BulkUpdateStatusAsync(BulkUpdateStatusDto dto, CancellationToken cancellationToken)
        {
            var response = new BulkUpdateResponseDto();
            var results = new List<BulkUpdateItemResultDto>();

            int successCount = 0;
            int failureCount = 0;

            var hasDuplicateIds = dto.Updates
                .GroupBy(update => update.Id)
                .Any(group => group.Count() > 1);

            if (hasDuplicateIds)
            {
                throw new ArgumentException(
                    "The request contains duplicate ticket IDs.");
            }
            var ticketIds = dto.Updates.Select(u => u.Id).Distinct().ToList();
            var existingTickets = await _ticketRepository.GetByIdsForUpdateAsync(ticketIds, cancellationToken);
            var ticketsDict = existingTickets.ToDictionary(t => t.Id);

            foreach (var updateItem in dto.Updates)
            {
                var itemResult = new BulkUpdateItemResultDto { Id = updateItem.Id };

                try
                {
                    if (string.IsNullOrWhiteSpace(updateItem.Status) || !TicketValidation.AllowedStatuses.Contains(updateItem.Status.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        itemResult.IsSuccess = false;
                        itemResult.Message = "Invalid status value. Allowed values: New, Waiting, InProgress, Completed.";
                        failureCount++;
                        results.Add(itemResult);
                        continue;
                    }
                    if (!ticketsDict.TryGetValue(updateItem.Id, out var ticket))
                    {
                        itemResult.IsSuccess = false;
                        itemResult.Message = $"Ticket with ID {updateItem.Id} was not found.";
                        failureCount++;
                        results.Add(itemResult);
                        continue;
                    }

                    byte[] rowVersionBytes;
                    try
                    {
                        rowVersionBytes = Convert.FromBase64String(updateItem.RowVersion);
                    }
                    catch
                    {
                        itemResult.IsSuccess = false;
                        itemResult.Message = "Invalid RowVersion format.";
                        failureCount++;
                        results.Add(itemResult);
                        continue;
                    }

                    _ticketRepository.SetOriginalRowVersion(ticket, rowVersionBytes);

                    ticket.Status = updateItem.Status.Trim();
                    ticket.UpdatedAt = DateTime.UtcNow;


                    await _ticketRepository.SaveChangesAsync(cancellationToken);

                    itemResult.IsSuccess = true;
                    itemResult.Message = "Updated successfully.";
                    successCount++;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (ticketsDict.TryGetValue(updateItem.Id, out var ticket))
                    {
                        _ticketRepository.DiscardFailedUpdate(ticket);
                    }

                    itemResult.IsSuccess = false;
                    itemResult.Message =
                        "Concurrency conflict: The ticket was modified by another user.";
                    failureCount++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    if (ticketsDict.TryGetValue(updateItem.Id, out var failedTicket))
                    {
                        _ticketRepository.DiscardFailedUpdate(failedTicket);
                    }

                    itemResult.IsSuccess = false;
                    itemResult.Message =
                        "An unexpected error occurred while updating the ticket.";

                    failureCount++;
                }
                results.Add(itemResult);
            }
            if (successCount > 0)
            {
                _cache.Remove(AggregationsCacheKey);
            }
            response.SuccessCount = successCount;
            response.FailureCount = failureCount;
            response.Results = results;

            return response;
        }
    }

}
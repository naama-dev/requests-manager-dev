using Microsoft.AspNetCore.Mvc;
using RequestsManager.Api.BLL.Interfaces;
using RequestsManager.Api.DTOs;
using RequestsManager.Api.Parameters;
using RequestsManager.Api.Validators;
namespace RequestsManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketsService _ticketsService; 

        public TicketsController(ITicketsService ticketsService)
        {
            _ticketsService = ticketsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets(
            [FromQuery] TicketQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            if (parameters.Page < 1)
            {
                return BadRequest(new
                {
                    message = "Page must be greater than or equal to 1."
                });
            }

            if (parameters.PageSize < 1 || parameters.PageSize > 100)
            {
                return BadRequest(new
                {
                    message = "PageSize must be between 1 and 100."
                });
            }

            if (parameters.FromDate.HasValue &&
                parameters.ToDate.HasValue &&
                parameters.FromDate.Value > parameters.ToDate.Value)
            {
                return BadRequest(new
                {
                    message = "FromDate must be earlier than or equal to ToDate."
                });
            }

            if (!string.IsNullOrWhiteSpace(parameters.Status) &&
                !TicketValidation.AllowedStatuses.Contains(
                    parameters.Status.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = $"Invalid Status. Allowed values: {string.Join(", ", TicketValidation.AllowedStatuses)}."
                });
            }

            if (!string.IsNullOrWhiteSpace(parameters.Priority) &&
                !TicketValidation.AllowedPriorities.Contains(
                    parameters.Priority.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = $"Invalid Priority. Allowed values: {string.Join(", ", TicketValidation.AllowedPriorities)}."
                });
            }

            var validSortFields = new[]
            {
        "CreatedAt",
        "UpdatedAt",
        "Title",
        "Status",
        "Priority",
        "OrganizationName",
        "AssignedTo"
    };
            parameters.Status = parameters.Status?.Trim();
            parameters.Priority = parameters.Priority?.Trim();
            parameters.AssignedTo = parameters.AssignedTo?.Trim();
            parameters.OrganizationName = parameters.OrganizationName?.Trim();
            parameters.Search = parameters.Search?.Trim();
            parameters.SortBy = string.IsNullOrWhiteSpace(parameters.SortBy)
          ? "CreatedAt"
          : parameters.SortBy.Trim();

            parameters.SortDirection = string.IsNullOrWhiteSpace(parameters.SortDirection)
                ? "Desc"
                : parameters.SortDirection.Trim();
            if (string.IsNullOrWhiteSpace(parameters.SortBy) ||
                !validSortFields.Contains(
                    parameters.SortBy.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = $"Invalid SortBy. Allowed values: {string.Join(", ", validSortFields)}."
                });
            }

            if (string.IsNullOrWhiteSpace(parameters.SortDirection) ||
                (!string.Equals(
                    parameters.SortDirection.Trim(),
                    "Asc",
                    StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(
                    parameters.SortDirection.Trim(),
                    "Desc",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new
                {
                    message = "SortDirection must be Asc or Desc."
                });
            }

            var result = await _ticketsService.GetTicketsAsync(
                parameters,
                cancellationToken);

            return Ok(result);
        }
        [HttpGet("aggregations")]
        public async Task<IActionResult> GetAggregations(
    CancellationToken cancellationToken)
        {
            var result = await _ticketsService.GetAggregationsAsync(
                cancellationToken);

            return Ok(result);
        }
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTicketStatus(
            int id,
            [FromBody] UpdateTicketStatusDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var updatedTicket = await _ticketsService.UpdateTicketStatusAsync(id, dto, cancellationToken);

                if (updatedTicket == null)
                {
                    return NotFound(new { message = $"Ticket with ID {id} was not found." });
                }

                return Ok(updatedTicket);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
   
      

        [HttpGet("{id}/history")]
        [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTicketHistory(int id, CancellationToken cancellationToken)
        {
            try
            {
                var history = await _ticketsService.GetTicketHistoryAsync(
                    id, cancellationToken);

                return Ok(history);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


       
        [HttpPut("bulk-status")]
        [ProducesResponseType(typeof(BulkUpdateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkUpdateStatus(
            [FromBody] BulkUpdateStatusDto dto,
            CancellationToken cancellationToken)
        {
            if (dto?.Updates == null ||
                !dto.Updates.Any() ||
                dto.Updates.Count > 100)
            {
                return BadRequest(new
                {
                    message = "Batch size must be between 1 and 100 items."
                });
            }

            try
            {
                var result = await _ticketsService.BulkUpdateStatusAsync(
                    dto, cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
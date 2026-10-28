
using System.ComponentModel.DataAnnotations;

namespace RequestsManager.Api.DTOs
{
    public class UpdateTicketStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

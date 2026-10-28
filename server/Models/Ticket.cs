using System;
using System.ComponentModel.DataAnnotations;

namespace RequestsManager.Api.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "New"; 

        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium"; 

        [MaxLength(100)]
        public string AssignedTo { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
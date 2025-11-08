using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class AddUserToClassDto
    {
        // Mandatory
        public string Name { get; set; }
        public string Email { get; set; }
        public Guid ClassId { get; set; }
        public string Status { get; set; } // Active, Inactive, Suspended

        // Optional
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

}

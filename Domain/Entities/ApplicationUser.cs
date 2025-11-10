using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Role { get; set; } = UserRole.User; // optional shortcut, still use Identity RoleManager
        public string? SchoolName { get; set; }
        public bool IsApproved { get; set; } = false;

        public Guid? SchoolId { get; set; } // FK for School
        public School School { get; set; }

        // Coach-only fields
        public string? Expertise { get; set; }   // Coach adds later
        public string? Bio { get; set; }         // Coach adds later
        public string? Achievements { get; set; } // Coach adds later
        public bool IsActive { get; set; } = true; // Admin/Coach can toggle
        public bool IsAvailable { get; set; } = true; // Coach updates based on schedule

        //student only fields
        public string? Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Status { get; set; } = "Active";


        //both coach and student
        public string? PhoneNumber { get; set; }

        //for qr code
        public string QrCodeValue { get; set; }
        public string QrCodeImageBase64 { get; set; }



    }
}

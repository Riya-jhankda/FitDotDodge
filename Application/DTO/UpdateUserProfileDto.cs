using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class UpdateUserProfileDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Location { get; set; }
        public string ProfilePictureUrl { get; set; }
    }

}

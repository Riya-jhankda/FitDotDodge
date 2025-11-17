using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class UpdateUserProfileDto
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
    }
}

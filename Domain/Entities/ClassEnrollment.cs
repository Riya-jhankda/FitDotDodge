using System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ClassEnrollment
    {
        [Key]
        public Guid EnrollmentId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public Guid ClassId { get; set; }
        public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active"; // Active / Inactive / Suspended

        // Navigation props
        public ApplicationUser User { get; set; }
        public Class Class { get; set; }
    }
}


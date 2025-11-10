using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Attendance
    {
        public Guid AttendanceId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public bool IsPresent { get; set; } = false;

        // Navigation
        public ApplicationUser User { get; set; }
        public Class Class { get; set; }
    }

}

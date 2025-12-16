using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class ManualAttendanceDto
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public List<StudentAttendanceItem> Students { get; set; }
    }

    public class StudentAttendanceItem
    {
        public string UserId { get; set; }
        public bool IsPresent { get; set; }
    }
}

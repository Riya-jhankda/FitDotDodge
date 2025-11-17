using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Class
    {
        public Guid ClassId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        

        [Required]
        public string CoachId { get; set; }
        public ApplicationUser Coach { get; set; }

        [Required]
        public ClassType ClassType { get; set; }

        public string SchoolName { get; set; }


        // ---- NEW FIELDS ----
        // Coach's short note / announcement for the class (overwritten on update)
        public string? CoachNote { get; set; }

        // Extra class info / description
        public string? Info { get; set; }

        public DateTime? NoteLastUpdated { get; set; }


    }

    public enum ClassType
    {
        Boxing,
        Zumba,
        Football,
        Muscle,
        Yoga,
        Cricket,
        Other
    }
}

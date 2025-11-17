using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class WorkoutLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string Category { get; set; }  // e.g. Arms, Legs, Chest, Back

        [Required]
        public string ExerciseName { get; set; }  // e.g. Bicep Curls

        public int Sets { get; set; }
        public int Reps { get; set; }
        public double? Weight { get; set; }  // Optional

        public DateTime LoggedDate { get; set; } = DateTime.UtcNow;
    }

}

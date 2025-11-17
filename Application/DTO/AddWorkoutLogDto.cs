using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class AddWorkoutLogDto
    {
        public string UserId { get; set; }
        public string Category { get; set; }
        public string ExerciseName { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public double? Weight { get; set; }
    }
}

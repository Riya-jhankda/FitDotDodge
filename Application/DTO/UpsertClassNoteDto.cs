using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    // DTO for create/update notes
    public class UpsertClassNoteDto
    {
        public string? CoachNote { get; set; }
        public string? Info { get; set; }
    }
}

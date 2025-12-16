using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class ScannerAttendanceDto
    {
        public string QrCodeValue { get; set; }   // user’s permanent QR
        public Guid ClassId { get; set; }         // class for which scanner is installed
    }
}


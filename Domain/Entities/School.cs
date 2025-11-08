using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class School
    {
        public Guid SchoolId { get; set; }
        public string Name { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<ScannerDevice> ScannerDevices { get; set; } = new List<ScannerDevice>();
    }

}

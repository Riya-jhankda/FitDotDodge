using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ScannerDevice
    {
        [Key]
        public Guid ScanId { get; set; }
        public string Name { get; set; }
        //public string ApiKeyHash { get; set; } // or ClientSecretHash
        //public bool IsRevoked { get; set; }
        //public DateTimeOffset? LastUsedAt { get; set; }
        public Guid SchoolId { get; set; }
        public School School { get; set; }

        public string ApiKey { get; set; }


    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime? Revoked { get; set; }
        public string? ReplacedByToken { get; set; }

        public string CreatedByIp { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
    }

}

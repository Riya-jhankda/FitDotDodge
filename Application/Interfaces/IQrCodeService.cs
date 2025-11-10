using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IQrCodeService
    {
        string GenerateQrValue(string userId);
        string GenerateQrImageBase64(string qrValue);
    }
}

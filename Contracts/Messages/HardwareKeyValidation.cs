using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Messages
{
    public sealed record HardwareKeyValidation(string HardwareKey, int UserId);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace Contracts.Messages
{
    public sealed record ProcessImage(string relativePath, int dbImageId);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models.DTO
{
    public class ImageSetDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string RelativePath { get; set; } = null!;
        public int FaceCount { get; set; }
        public string[]? Labels { get; set; }
    }
}

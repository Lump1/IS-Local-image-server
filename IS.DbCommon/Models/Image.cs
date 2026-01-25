using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models
{
    public class Image
    {
        public int Id { get; set; }

        public string FileName { get; set; } = null!;
        public string RelativePath { get; set; } = null!;
        public string? PerceptualHash { get; set; }

        public int FaceCount { get; set; }

        // PostgreSQL: text[]
        public string[]? Labels { get; set; }

        public int PostedByAccountId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Навигация
        public Account PostedByAccount { get; set; } = null!;
        public Metadata Metadata { get; set; } = null!;
        public ICollection<Face> Faces { get; set; } = new List<Face>();
    }
}

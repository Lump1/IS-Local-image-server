using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models
{
    public class Face
    {
        public int Id { get; set; }

        public int PhotoId { get; set; }             // FK -> Image.Id
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public byte[]? Embedding { get; set; }       // вектор признаков
        public int? PersonId { get; set; }
        public float Confidence { get; set; }

        public DateTime CreatedAt { get; set; }

        // Навигация
        public Image Photo { get; set; } = null!;
        public Person? Person { get; set; }
    }

}

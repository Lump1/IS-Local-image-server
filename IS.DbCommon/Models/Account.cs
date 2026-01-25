using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models
{
    public class Account
    {
        public int Id { get; set; }

        public string Login { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        // PostgreSQL text[]
        public string[]? Permissions { get; set; }

        public DateTime CreatedAt { get; set; }

        // Навигация
        public Person? Person { get; set; }
        public ICollection<Image> PostedImages { get; set; } = new List<Image>();
    }

}

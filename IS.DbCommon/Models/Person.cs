using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models
{
    public class Person
    {
        public int Id { get; set; }

        public int? AccountId { get; set; }          // acc_id?
        public string DisplayName { get; set; } = null!;
        public int? FaceId { get; set; }             // основное лицо (аватарка)

        public DateTime CreatedAt { get; set; }

        // Навигация
        public Account? Account { get; set; }
        public Face? MainFace { get; set; }          // навигация по FaceId
        public ICollection<Face> Faces { get; set; } = new List<Face>();
    }

}

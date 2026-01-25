using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models.DTO
{
    public sealed class UploadImageDto
    {
        /// <summary>Image file (multipart/form-data)</summary>
        [Required]
        public IFormFile File { get; init; } = default!;

        /// <summary>Tags/labels (PostgreSQL text[])</summary>
        public string[]? Labels { get; init; }

        /// <summary>Account ID</summary>
        public int? PostedByAccountId { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models
{
    public class Metadata
    {
        // PK + FK -> Image.Id
        public int PhotoId { get; set; }

        // Файл
        public string? OriginalFileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string MimeType { get; set; } = null!;
        public string? Extension { get; set; }

        // Размер / ориентация
        public int? Width { get; set; }
        public int? Height { get; set; }
        public short? Orientation { get; set; }

        // Дата съёмки
        public DateTime? TakenAt { get; set; }
        public DateTime? TakenAtUtc { get; set; }

        // Устройство
        public string? CameraMake { get; set; }
        public string? CameraModel { get; set; }
        public string? LensModel { get; set; }

        // Настройки съёмки
        public float? FocalLengthMm { get; set; }
        public int? Iso { get; set; }
        public string? ExposureTime { get; set; }   // "1/120" и т.п.
        public float? FNumber { get; set; }
        public bool? FlashFired { get; set; }

        // Гео
        public double? GpsLatitude { get; set; }
        public double? GpsLongitude { get; set; }
        public double? GpsAltitude { get; set; }
        public string? LocationCountry { get; set; }
        public string? LocationCity { get; set; }

        // Хэши / служебное
        public string? HashSha1 { get; set; }

        public int FacesDetected { get; set; }
        public bool FacesScanned { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Навигация
        public Image Image { get; set; } = null!;
    }

}

using Api;
using IS.DbCommon.Models.DTO;
using System.Text.Json.Serialization;

namespace IS.ImageService.Api.Services.SerializationService
{
    [JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
    [JsonSerializable(typeof(ImageSetDto))]
    [JsonSerializable(typeof(List<ImageSetDto>))]
    public partial class httpRequestsSerializationJson
    {

    }
}

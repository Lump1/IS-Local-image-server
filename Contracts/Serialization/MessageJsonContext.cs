using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contracts.Serialization
{
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = false)]
    [JsonSerializable(typeof(Contracts.Messages.GetImagesCount))]
    [JsonSerializable(typeof(Contracts.Messages.ProcessImage))]
    [JsonSerializable(typeof(Contracts.Messages.HardwareKeyValidation))]
    [JsonSerializable(typeof(Contracts.Messages.CacheHandlerSetter))]
    public partial class MessageJsonContext : JsonSerializerContext
    {
    }
}

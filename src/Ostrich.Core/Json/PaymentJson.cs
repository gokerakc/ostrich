using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ostrich.Core.Json;

public static class PaymentJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

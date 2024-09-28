using System.Text.Json;

namespace SUIA.Shared.Utilities;
public static class JsonExtension
{
    private static JsonSerializerOptions _jso = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static string ToJson<T>(this T source) => JsonSerializer.Serialize(source, _jso);
    public static T FromJson<T>(this string source) => JsonSerializer.Deserialize<T>(source, _jso) ?? default!;
    public static T Clone<T>(this T source) => source.ToJson().FromJson<T>();
}
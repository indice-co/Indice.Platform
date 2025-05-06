using System.Dynamic;
using System.Text.Json;

namespace Indice.Features.Cases.Server;

public static class JsonToExpandoConverter
{
    public static ExpandoObject ConvertToExpando(string json)
    {
        using var document = JsonDocument.Parse(json);
        return (ExpandoObject)ConvertElement(document.RootElement);
    }

    private static object ConvertElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var expando = new ExpandoObject() as IDictionary<string, object>;
                foreach (var property in element.EnumerateObject())
                {
                    expando[property.Name] = ConvertElement(property.Value);
                }
                return expando;

            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertElement(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l))
                    return l;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                throw new NotSupportedException($"Unsupported JsonValueKind: {element.ValueKind}");
        }
    }
}
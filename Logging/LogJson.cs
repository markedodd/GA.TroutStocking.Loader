using System.Text.Encodings.Web;
using System.Text.Json;

namespace GA_TroutStocking_Loader;

internal static class LogJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Message(string message, object? data = null)
    {
        // Returns a JSON value (object) that can be embedded directly into the layout.
        return JsonSerializer.Serialize(new
        {
            message,
            data
        }, Options);
    }

    public static string Exception(Exception? ex)
    {
        if (ex is null)
        {
            return "null";
        }

        return JsonSerializer.Serialize(new
        {
            type = ex.GetType().FullName,
            ex.Message,
            ex.StackTrace
        }, Options);
    }
}
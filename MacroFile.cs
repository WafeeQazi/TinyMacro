using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinyMacro;

public static class MacroFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void Save(string path, MacroData data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(path, json);
    }

    public static MacroData Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MacroData>(json, Options) ?? new MacroData();
    }
}

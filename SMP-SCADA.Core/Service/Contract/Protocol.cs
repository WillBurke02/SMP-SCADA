using System.Text.Json.Serialization;
using System.Text.Json;

namespace SMP_SCADA.Core.Service.Contract;

public static class Protocol
{
    public const int Version = 1;
    public const string PipeName = "SMP_SCADA";
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };
    public static string Write(ServiceMessage message) => JsonSerializer.Serialize(message, Options);
    public static string Write(Command command) => JsonSerializer.Serialize(command, Options);

    public static ServiceMessage ReadServiceMessage(string line) => Read<ServiceMessage>(line);
    public static Command ReadCommand(string line) => Read<Command>(line);
    static T Read<T>(string line) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(line, Options) ?? throw new JsonException("Empty message");
        }
        catch (NotSupportedException e)
        {
            throw new JsonException("Message has no type", e);
        }
    }
}
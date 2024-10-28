using System.Text.Json.Serialization;

namespace Verify.Application.Dtos.Common;
public record DhtResponse<T>
{
    public bool Successful { get; init; } = true;
    public string? Message { get; init; } = "Operation Successful";
    public T? Data { get; init; }
    public Exception? Exception { get; init; }

    // To hold any extra information
    public Dictionary<string, object>? AdditionalData { get; init; } = new Dictionary<string, object>();


    [JsonConstructor]
    private DhtResponse(bool successful, string? message, T? data, Exception? exception, Dictionary<string, object>? additionalData)
    {
        Successful = successful;
        Message = message;
        Data = data;
        Exception = exception;
        AdditionalData = additionalData;
    }

    public static DhtResponse<T> Success(string message, T value, Exception? exception = null, Dictionary<string, object>? additionalData = null)
    {
        return new DhtResponse<T>(true, message, value, null, additionalData);
    }

    public static DhtResponse<T> Failure(string errorMessage, T? data = default, Exception? error = null, Dictionary<string, object>? additionalData = null)
    {
        return new DhtResponse<T>(false, errorMessage, data, error, additionalData);
    }

}

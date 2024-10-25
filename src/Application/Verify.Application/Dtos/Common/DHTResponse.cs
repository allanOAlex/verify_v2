using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Common;
public record DHTResponse<T>
{
    public bool Successful { get; init; } = true;
    public string? Message { get; init; } = "Operation Successful";
    public T? Data { get; init; }
    public Exception? Exception { get; init; }

    // To hold any extra information
    public Dictionary<string, object>? AdditionalData { get; init; } = new Dictionary<string, object>();


    [JsonConstructor]
    private DHTResponse(bool successful, string? message, T? data, Exception? exception, Dictionary<string, object>? additionalData)
    {
        Successful = successful;
        Message = message;
        Data = data;
        Exception = exception;
        AdditionalData = additionalData;
    }

    public static DHTResponse<T> Success(string message, T value, Exception? exception = null, Dictionary<string, object>? additionalData = null)
    {
        return new DHTResponse<T>(true, message, value, null, additionalData);
    }

    public static DHTResponse<T> Failure(string errorMessage, T? data = default, Exception? error = null, Dictionary<string, object>? additionalData = null)
    {
        return new DHTResponse<T>(false, errorMessage, data, error, additionalData);
    }

}

using System.Text.Json.Serialization;

namespace Verify.Application.Dtos.Common;

public record Response<T>
{
    public bool Successful { get; init; } = true;
    public string? Message { get; init; } = "Operation Successful";
    public T? Data { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages { get; init; }
    public int PageNumber { get; init; }
    public int Cursor { get; init; }
    public int PreviousCursor { get; init; }
    public int NextCursor { get; init; }
    public int LastCursor { get; init; }

    public Exception? Exception { get; init; }

    // To hold any extra information
    public Dictionary<string, object>? AdditionalData { get; init; } = new Dictionary<string, object>();


    [JsonConstructor]
    private Response(bool successful, string? message, T? data, int totalRecords, int totalPages, int pageNumber, int cursor, int previousCursor, int nextCursor, int lastCursor, Exception? exception, Dictionary<string, object>? additionalData)
    {
        Successful = successful;
        Message = message;
        Data = data;
        TotalRecords = totalRecords;
        TotalPages = totalPages;
        PageNumber = pageNumber;
        Cursor = cursor;
        PreviousCursor = previousCursor;
        NextCursor = nextCursor;
        LastCursor = lastCursor;
        Exception = exception;
        AdditionalData = additionalData;
    }


    public static Response<T> Success(string message, T value, int totalRecords = 0, int totalPages = 0, int pageNumber = 0, int cursor = 0, int previousCursor = 0, int nextCursor = 0, int lastCursor = 0, Exception? exception = null, Dictionary<string, object>? additionalData = null)
    {
        return new Response<T>(true, message, value, totalRecords, totalPages, pageNumber, cursor, previousCursor, nextCursor, lastCursor, new Exception(), additionalData);
    }

    public static Response<T> Failure(string errorMessage, T? data = default, Exception? error = null, Dictionary<string, object>? additionalData = null)
    {
        return new Response<T>(false, errorMessage, data, 0, 0, 0, 0, 0, 0, 0, error, additionalData);
    }


}

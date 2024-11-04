using System.Net;

namespace Verify.Shared.Exceptions;
public class CustomException : Exception
{
    public List<string>? ErrorMessages { get; }
    public HttpStatusCode StatusCode { get; }

    public CustomException()
    {

    }

    public CustomException(
        string message,
        List<string>? errorMessages = default,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : base(message)
    {
        ErrorMessages = errorMessages;
        StatusCode = statusCode;
    }

    public CustomException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

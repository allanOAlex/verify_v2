namespace Verify.Shared.Exceptions;
public class NoContentException : CustomException
{
    public NoContentException()
    {

    }

    public NoContentException(string message) : base(message) { }

    public NoContentException(string message, Exception innerException) : base(message, innerException) { }
}

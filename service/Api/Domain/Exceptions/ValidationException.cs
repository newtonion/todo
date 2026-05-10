namespace Api.Domain.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("Validation error")
    {
    }

    public ValidationException(string message)
        : base(message)
    {
    }
}

namespace LayeredArchitecture.BLL.Exceptions;

public class ValidationException : BusinessException
{
    public ValidationException(string message) : base(message) { }
    
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
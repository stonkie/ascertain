namespace Ascertain.Compiler;

public class AscertainException : Exception
{
    public AscertainErrorCode ErrorCode { get; }
    public string ErrorDetails { get; }

    public AscertainException(AscertainErrorCode errorCode, string errorDetails)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
}
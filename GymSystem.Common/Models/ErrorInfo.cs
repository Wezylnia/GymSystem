namespace GymSystem.Common.Models;

public class ErrorInfo {
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? StackTrace { get; set; }
    public int StatusCode { get; set; } = 500;

    public ErrorInfo() { }

    public ErrorInfo(string errorMessage, string? errorCode = null, int statusCode = 500) {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public ErrorInfo(string errorMessage, string? errorCode, string? stackTrace, int statusCode = 500) {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        StackTrace = stackTrace;
        StatusCode = statusCode;
    }
}

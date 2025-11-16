using GymSystem.Common.Models;
using Microsoft.Extensions.Logging;

namespace GymSystem.Common.Helpers;

public class ServiceResponseHelper : IServiceResponseHelper
{
    private readonly ILogger<ServiceResponseHelper> _logger;

    public ServiceResponseHelper(ILogger<ServiceResponseHelper> logger)
    {
        _logger = logger;
    }

    public ServiceResponse SetSuccess()
    {
        return new ServiceResponse(isSuccessful: true);
    }

    public ServiceResponse SetSuccess(string message)
    {
        return new ServiceResponse(isSuccessful: true, message: message);
    }

    public ServiceResponse<T> SetSuccess<T>(T data)
    {
        return new ServiceResponse<T>(data, isSuccessful: true);
    }

    public ServiceResponse<T> SetSuccess<T>(T data, string message)
    {
        return new ServiceResponse<T>(data, isSuccessful: true, message: message);
    }

    public ServiceResponse SetError(string errorMessage, int statusCode = 500, string? errorCode = null)
    {
        var errorInfo = new ErrorInfo(errorMessage, errorCode, statusCode);
        
        _logger.LogError("Service Error: {ErrorMessage} | StatusCode: {StatusCode} | ErrorCode: {ErrorCode}", 
            errorMessage, statusCode, errorCode);

        return new ServiceResponse(isSuccessful: false, error: errorInfo);
    }

    public ServiceResponse SetError(ErrorInfo errorInfo)
    {
        _logger.LogError("Service Error: {ErrorMessage} | StatusCode: {StatusCode} | ErrorCode: {ErrorCode}", 
            errorInfo.ErrorMessage, errorInfo.StatusCode, errorInfo.ErrorCode);

        return new ServiceResponse(isSuccessful: false, error: errorInfo);
    }

    public ServiceResponse<T> SetError<T>(T? data, string errorMessage, int statusCode = 500, string? errorCode = null)
    {
        var errorInfo = new ErrorInfo(errorMessage, errorCode, statusCode);
        
        _logger.LogError("Service Error: {ErrorMessage} | StatusCode: {StatusCode} | ErrorCode: {ErrorCode}", 
            errorMessage, statusCode, errorCode);

        return new ServiceResponse<T>(data, isSuccessful: false, error: errorInfo);
    }

    public ServiceResponse<T> SetError<T>(T? data, ErrorInfo errorInfo)
    {
        _logger.LogError("Service Error: {ErrorMessage} | StatusCode: {StatusCode} | ErrorCode: {ErrorCode} | StackTrace: {StackTrace}", 
            errorInfo.ErrorMessage, errorInfo.StatusCode, errorInfo.ErrorCode, errorInfo.StackTrace);

        return new ServiceResponse<T>(data, isSuccessful: false, error: errorInfo);
    }
}

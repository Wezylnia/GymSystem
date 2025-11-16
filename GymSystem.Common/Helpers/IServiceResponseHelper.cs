using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Common.Helpers;

/// <summary>
/// ServiceResponse oluşturmak için helper interface
/// IApplicationService'den türer - otomatik service registration için
/// </summary>
public interface IServiceResponseHelper : IApplicationService
{
    ServiceResponse SetSuccess();
    ServiceResponse SetSuccess(string message);
    ServiceResponse<T> SetSuccess<T>(T data);
    ServiceResponse<T> SetSuccess<T>(T data, string message);
    
    ServiceResponse SetError(string errorMessage, int statusCode = 500, string? errorCode = null);
    ServiceResponse SetError(ErrorInfo errorInfo);
    ServiceResponse<T> SetError<T>(T? data, string errorMessage, int statusCode = 500, string? errorCode = null);
    ServiceResponse<T> SetError<T>(T? data, ErrorInfo errorInfo);
}

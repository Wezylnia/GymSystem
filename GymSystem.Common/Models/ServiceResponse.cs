namespace GymSystem.Common.Models;

/// <summary>
/// Generic olmayan ServiceResponse - sadece başarı/hata durumu için
/// </summary>
public class ServiceResponse {
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public ErrorInfo? Error { get; set; }

    public ServiceResponse() {
        IsSuccessful = true;
    }

    public ServiceResponse(bool isSuccessful, string? message = null, ErrorInfo? error = null) {
        IsSuccessful = isSuccessful;
        Message = message;
        Error = error;
    }
}

/// <summary>
/// Generic ServiceResponse - data ile birlikte başarı/hata durumu için
/// </summary>
public class ServiceResponse<T> : ServiceResponse {
    public T? Data { get; set; }

    public ServiceResponse() : base() {
    }

    public ServiceResponse(T? data, bool isSuccessful = true, string? message = null, ErrorInfo? error = null)
        : base(isSuccessful, message, error) {
        Data = data;
    }
}
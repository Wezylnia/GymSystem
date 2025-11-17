namespace GymSystem.Application.Abstractions.Services;

public interface IGeminiApiService
{
    /// <summary>
    /// Gemini API ile workout planı oluşturur
    /// </summary>
    Task<string> GenerateWorkoutPlanAsync(decimal height, decimal weight, string? bodyType, string goal, string? photoBase64 = null);

    /// <summary>
    /// Gemini API ile diet planı oluşturur
    /// </summary>
    Task<string> GenerateDietPlanAsync(decimal height, decimal weight, string? bodyType, string goal, string? photoBase64 = null);

    /// <summary>
    /// Gemini Vision API ile fotoğraf analizi yapar
    /// </summary>
    Task<string> AnalyzeBodyPhotoAsync(string photoBase64, decimal height, decimal weight, string goal);
}

using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan.Contract;
using GymSystem.Common.Models;

namespace GymSystem.Application.Abstractions.Services.IGemini;

public interface IGeminiApiService {
    /// <summary>
    /// Gemini API ile workout planı oluşturur
    /// </summary>
    Task<ServiceResponse<string>> GenerateWorkoutPlanAsync(AIWorkoutPlanDto request);

    /// <summary>
    /// Gemini API ile diet planı oluşturur
    /// </summary>
    Task<ServiceResponse<string>> GenerateDietPlanAsync(AIWorkoutPlanDto request);

    /// <summary>
    /// Gemini Vision API ile fotoğraf analizi yapar
    /// </summary>
    Task<ServiceResponse<string>> AnalyzeBodyPhotoAsync(AIWorkoutPlanDto request);

    /// <summary>
    /// Gemini Imagen API ile 6 ay sonraki hedef vücut görselini oluşturur
    /// </summary>
    Task<ServiceResponse<string>> GenerateFutureBodyImageAsync(AIWorkoutPlanDto request);
}

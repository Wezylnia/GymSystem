using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan.Contract;
using GymSystem.Application.Abstractions.Services.IGemini;
using GymSystem.Application.Services.AI.Helpers;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GymSystem.Application.Services.AI;

public class GeminiApiService : IGeminiApiService {
    private readonly BaseFactory<GeminiApiService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<GeminiApiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta";
    private const string GEMINI_TEXT_MODEL = "gemini-2.0-flash-exp"; // Text generation
    private const string GEMINI_IMAGE_MODEL = "gemini-2.5-flash-image"; // Image generation

    public GeminiApiService(BaseFactory<GeminiApiService> baseFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _httpClient = httpClientFactory.CreateClient("GeminiApi");
        _apiKey = configuration["AI:Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key not configured");
    }

    public async Task<ServiceResponse<string>> GenerateWorkoutPlanAsync(AIWorkoutPlanDto request) {
        try {
            var prompt = GeminiPromptHelper.BuildWorkoutPrompt(request.Height, request.Weight, request.Gender, request.BodyType, request.Goal);
            return await GenerateTextAsync(prompt);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Workout planı oluşturulurken hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Workout planı oluşturulamadı", "GEMINI_WORKOUT_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<string>> GenerateDietPlanAsync(AIWorkoutPlanDto request) {
        try {
            var prompt = GeminiPromptHelper.BuildDietPrompt(request.Height, request.Weight, request.Gender, request.BodyType, request.Goal);
            return await GenerateTextAsync(prompt);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Diet planı oluşturulurken hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Diet planı oluşturulamadı", "GEMINI_DIET_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<string>> AnalyzeBodyPhotoAsync(AIWorkoutPlanDto request) {
        try {
            if (string.IsNullOrEmpty(request.PhotoBase64))
                return _responseHelper.SetError<string>(null, "Fotoğraf analizi için fotoğraf gereklidir", 400, "GEMINI_ANALYSIS_002");

            var prompt = GeminiPromptHelper.BuildBodyAnalysisPrompt(request.Height, request.Weight, request.Gender, request.Goal);
            return await GenerateWithVisionAsync(prompt, request.PhotoBase64);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Fotoğraf analizi sırasında hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Fotoğraf analizi yapılamadı", "GEMINI_ANALYSIS_001", ex.StackTrace, 500));
        }
    }

    private async Task<ServiceResponse<string>> GenerateTextAsync(string prompt) {
        var url = $"{API_BASE_URL}/models/{GEMINI_TEXT_MODEL}:generateContent";

        var requestBody = new {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.4, topK = 20, topP = 0.8, maxOutputTokens = 2048 }
        };

        try {
            _logger.LogInformation("Gemini Text API'ye istek gönderiliyor: {Model}", GEMINI_TEXT_MODEL);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gemini API yanıtı: {StatusCode}, İçerik uzunluğu: {Length}", response.StatusCode, result.Length);

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Gemini API hatası: {Status} - {Content}", response.StatusCode, result);
                return _responseHelper.SetError<string>(null, $"Gemini API hatası: {response.StatusCode}", (int)response.StatusCode, "GEMINI_API_ERROR");
            }

            var jsonDoc = JsonDocument.Parse(result);
            var text = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text))
                return _responseHelper.SetError<string>(null, "AI'dan boş plan döndü", 500, "GEMINI_EMPTY_RESPONSE");

            return _responseHelper.SetSuccess<string>(text);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Gemini API çağrısında hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Gemini API çağrısında hata oluştu", "GEMINI_REQUEST_ERROR", ex.StackTrace, 500));
        }
    }

    private async Task<ServiceResponse<string>> GenerateWithVisionAsync(string prompt, string photoBase64) {
        var url = $"{API_BASE_URL}/models/{GEMINI_TEXT_MODEL}:generateContent";

        var base64Data = photoBase64.Contains(",") ? photoBase64.Split(',')[1] : photoBase64;

        var requestBody = new {
            contents = new[] { new { parts = new object[] { new { text = prompt }, new { inline_data = new { mime_type = "image/jpeg", data = base64Data } } } } },
            generationConfig = new { temperature = 0.4, topK = 20, topP = 0.8, maxOutputTokens = 2048 }
        };

        try {
            _logger.LogInformation("Gemini Vision API'ye istek gönderiliyor: {Model}", GEMINI_TEXT_MODEL);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gemini Vision API yanıtı: {StatusCode}, İçerik uzunluğu: {Length}", response.StatusCode, result.Length);

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Gemini Vision API hatası: {Status} - {Content}", response.StatusCode, result);
                return _responseHelper.SetError<string>(null, $"Gemini Vision API hatası: {response.StatusCode}", (int)response.StatusCode, "GEMINI_VISION_API_ERROR");
            }

            var jsonDoc = JsonDocument.Parse(result);
            var text = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text))
                return _responseHelper.SetError<string>(null, "AI'dan boş plan döndü", 500, "GEMINI_VISION_EMPTY_RESPONSE");

            return _responseHelper.SetSuccess<string>(text);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Gemini Vision API çağrısında hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Gemini Vision API çağrısında hata oluştu", "GEMINI_VISION_REQUEST_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<string>> GenerateFutureBodyImageAsync(AIWorkoutPlanDto request) {
        try {
            var prompt = GeminiPromptHelper.BuildFutureBodyImagePrompt(request.Gender, request.Goal);
            return await GenerateImageAsync(prompt);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hedef vücut görseli oluşturulurken hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Hedef vücut görseli oluşturulamadı", "GEMINI_IMAGE_002", ex.StackTrace, 500));
        }
    }

    private async Task<ServiceResponse<string>> GenerateImageAsync(string prompt) {
        // Gemini 2.5 Flash Image Generation
        var url = $"{API_BASE_URL}/models/{GEMINI_IMAGE_MODEL}:generateContent";

        var requestBody = new {
            contents = new[] {
                new {
                    parts = new[] {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new {
                responseModalities = new[] { "image", "text" }
            }
        };

        try {
            _logger.LogInformation("Gemini Image API'ye istek gönderiliyor: {Model}, Prompt: {Prompt}", GEMINI_IMAGE_MODEL, prompt);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gemini Image API yanıtı: {StatusCode}, Response length: {Length}", response.StatusCode, result.Length);

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Gemini Image API hatası: {Status} - {Content}", response.StatusCode, result);
                return _responseHelper.SetError<string>(null, $"Görsel oluşturma hatası: {response.StatusCode}", (int)response.StatusCode, "GEMINI_IMAGE_API_ERROR");
            }

            var jsonDoc = JsonDocument.Parse(result);

            // Candidates kontrolü
            if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0) {
                var candidate = candidates[0];

                // Safety check
                if (candidate.TryGetProperty("finishReason", out var finishReason)) {
                    var reason = finishReason.GetString();
                    if (reason == "IMAGE_SAFETY" || reason == "SAFETY") {
                        _logger.LogWarning("Görsel güvenlik nedeniyle engellendi: {Reason}", reason);
                        return _responseHelper.SetError<string>(null, "Görsel oluşturulamadı. Lütfen tekrar deneyin.", 400, "GEMINI_SAFETY_BLOCK");
                    }
                }

                if (candidate.TryGetProperty("content", out var content)) {
                    var parts = content.GetProperty("parts");

                    // Önce inlineData (görsel) ara
                    foreach (var part in parts.EnumerateArray()) {
                        if (part.TryGetProperty("inlineData", out var inlineData)) {
                            var mimeType = inlineData.GetProperty("mimeType").GetString();
                            var imageData = inlineData.GetProperty("data").GetString();

                            if (!string.IsNullOrEmpty(imageData)) {
                                var fullBase64 = $"data:{mimeType};base64,{imageData}";
                                _logger.LogInformation("Gemini ile görsel başarıyla oluşturuldu");
                                return _responseHelper.SetSuccess<string>(fullBase64);
                            }
                        }
                    }
                }
            }

            _logger.LogWarning("Görsel oluşturulamadı, tam yanıt: {Result}", result.Length > 2000 ? result.Substring(0, 2000) : result);
            return _responseHelper.SetError<string>(null, "AI görsel oluşturamadı. Lütfen tekrar deneyin.", 500, "GEMINI_NO_IMAGE");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Gemini Image API çağrısında hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Görsel oluşturma hatası", "GEMINI_IMAGE_REQUEST_ERROR", ex.StackTrace, 500));
        }
    }
}
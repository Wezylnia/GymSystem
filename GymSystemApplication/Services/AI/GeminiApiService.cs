using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan.Contract;
using GymSystem.Application.Abstractions.Services.IGemini;
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

    public GeminiApiService(BaseFactory<GeminiApiService> baseFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _httpClient = httpClientFactory.CreateClient("GeminiApi");
        _apiKey = configuration["AI:Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key not configured");
    }

    public async Task<ServiceResponse<string>> GenerateWorkoutPlanAsync(AIWorkoutPlanDto request) {
        try {
            var prompt = BuildWorkoutPrompt(request.Height, request.Weight, request.Gender, request.BodyType, request.Goal);

            if (!string.IsNullOrEmpty(request.PhotoBase64))
                return await GenerateWithVisionAsync(prompt, request.PhotoBase64);

            return await GenerateTextAsync(prompt);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Workout planı oluşturulurken hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Workout planı oluşturulamadı", "GEMINI_WORKOUT_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<string>> GenerateDietPlanAsync(AIWorkoutPlanDto request) {
        try {
            var prompt = BuildDietPrompt(request.Height, request.Weight, request.Gender, request.BodyType, request.Goal);

            if (!string.IsNullOrEmpty(request.PhotoBase64))
                return await GenerateWithVisionAsync(prompt, request.PhotoBase64);

            return await GenerateTextAsync(prompt);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Diet planı oluşturulurken hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Diet planı oluşturulamadı", "GEMINI_DIET_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<string>> AnalyzeBodyPhotoAsync(AIWorkoutPlanDto request) {
        try {
            var genderText = request.Gender == Domain.Enums.Gender.Female ? "Kadın" : "Erkek";
            var prompt = $@"Bu fotoğraftaki kişinin fiziksel durumunu analiz et.
Kişi Bilgileri:
- Cinsiyet: {genderText}
- Boy: {request.Height} cm
- Kilo: {request.Weight} kg
- Hedef: {request.Goal}

Lütfen şu bilgileri ver:
1. Vücut tipi analizi (ectomorph/mesomorph/endomorph)
2. Güncel fiziksel durum değerlendirmesi
3. Hedefine ulaşmak için öneriler
4. Tahmini hedefe ulaşma süresi

Türkçe olarak detaylı bir analiz yap.";

            if (string.IsNullOrEmpty(request.PhotoBase64))
                return _responseHelper.SetError<string>(null, "Fotoğraf analizi için fotoğraf gereklidir", 400, "GEMINI_ANALYSIS_002");

            return await GenerateWithVisionAsync(prompt, request.PhotoBase64);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Fotoğraf analizi sırasında hata oluştu");
            return _responseHelper.SetError<string>(null, new ErrorInfo("Fotoğraf analizi yapılamadı", "GEMINI_ANALYSIS_001", ex.StackTrace, 500));
        }
    }

    private async Task<ServiceResponse<string>> GenerateTextAsync(string prompt) {
        var url = $"{API_BASE_URL}/models/gemini-2.0-flash-exp:generateContent";

        var requestBody = new {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.4, topK = 20, topP = 0.8, maxOutputTokens = 2048 }
        };

        try {
            _logger.LogInformation("Gemini API'ye istek gönderiliyor: {Url}", url);

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
        var url = $"{API_BASE_URL}/models/gemini-2.0-flash-exp:generateContent";

        var base64Data = photoBase64.Contains(",") ? photoBase64.Split(',')[1] : photoBase64;

        var requestBody = new {
            contents = new[] { new { parts = new object[] { new { text = prompt }, new { inline_data = new { mime_type = "image/jpeg", data = base64Data } } } } },
            generationConfig = new { temperature = 0.4, topK = 20, topP = 0.8, maxOutputTokens = 2048 }
        };

        try {
            _logger.LogInformation("Gemini Vision API'ye istek gönderiliyor: {Url}", url);

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

    private string BuildWorkoutPrompt(decimal height, decimal weight, Domain.Enums.Gender gender, string? bodyType, string goal) {
        var bmi = weight / ((height / 100) * (height / 100));
        var genderText = gender == Domain.Enums.Gender.Female ? "Kadın" : "Erkek";

        return $@"Sen bir fitness koçusun. Aşağıdaki bilgilere göre KISA ve ÖZ bir haftalık egzersiz planı oluştur.

Bilgiler:
- Cinsiyet: {genderText}
- Boy: {height} cm, Kilo: {weight} kg, BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiş"}
- Hedef: {goal}

ÖNEMLİ: Cinsiyete uygun egzersizler seç. {(gender == Domain.Enums.Gender.Female ? "Kadınlar için özellikle alt vücut, kalça ve bacak egzersizlerine odaklan. Ağırlıkları kadınlar için uygun seç." : "Erkekler için göğüs, omuz ve kol egzersizlerine ağırlık ver. Daha yüksek ağırlıklarla çalışılabilir.")}

SADECE ŞU FORMATTA YAZ (gereksiz açıklama yapma):

📊 DURUM ANALİZİ
BMI: {bmi:F2} - [değerlendirme 1 cümle]

💪 HAFTALIK EGZERSİZ PLANI

PAZARTESİ - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12
• Egzersiz 3: 3x10

SALI - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12

ÇARŞAMBA - Dinlenme

PERŞEMBE - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12

CUMA - [Kas Grubu]
• Egzersiz 1: 3x12
• Egzersiz 2: 3x12

CUMARTESİ - Cardio veya Dinlenme

PAZAR - Dinlenme

🍎 BESLENME ÖNERİSİ
Günlük kalori: [miktar] kcal
Protein: [miktar]g | Karbonhidrat: [miktar]g | Yağ: [miktar]g

⚠️ ÖNEMLİ NOTLAR
• [Not 1]
• [Not 2]

Türkçe yaz. Kısa ve net ol. Gereksiz açıklama yapma!";
    }

    private string BuildDietPrompt(decimal height, decimal weight, Domain.Enums.Gender gender, string? bodyType, string goal) {
        var bmi = weight / ((height / 100) * (height / 100));
        var genderText = gender == Domain.Enums.Gender.Female ? "Kadın" : "Erkek";

        return $@"Sen bir beslenme uzmanısın. Aşağıdaki bilgilere göre KISA ve ÖZ bir haftalık diyet planı oluştur.

Bilgiler:
- Cinsiyet: {genderText}
- Boy: {height} cm, Kilo: {weight} kg, BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiş"}
- Hedef: {goal}

ÖNEMLİ: Cinsiyete uygun kalori ve makro besin hesapla. {(gender == Domain.Enums.Gender.Female ? "Kadınlar için genelde 1500-2000 kcal aralığında, demir ve kalsiyum içeren besinlere odaklan." : "Erkekler için genelde 2000-2500 kcal aralığında, protein ağırlıklı beslenme öner.")}

SADECE ŞU FORMATTA YAZ (gereksiz açıklama yapma):

📊 BESİN ANALİZİ
Günlük kalori: [miktar] kcal
Protein: [miktar]g | Karbonhidrat: [miktar]g | Yağ: [miktar]g

🍽️ HAFTALIK DİYET PLANI

PAZARTESİ
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

SALI
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

ÇARŞAMBA
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

PERŞEMBE
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

CUMA
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

CUMARTESİ
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

PAZAR
Kahvaltı: [yiyecek] - [kalori]kcal
Öğle: [yiyecek] - [kalori]kcal
Akşam: [yiyecek] - [kalori]kcal

💊 SUPPLEMENT
• [takviye 1]
• [takviye 2]

💧 SU: Günde en az 2.5 litre

⚠️ ÖNEMLİ NOTLAR
• [Not 1]
• [Not 2]

Türkçe yaz. Kısa ve net ol. Gereksiz açıklama yapma!";
    }
}
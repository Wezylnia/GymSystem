using GymSystem.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GymSystem.Application.Services.AI;

public class GeminiApiService : IGeminiApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiApiService> _logger;
    private readonly string _apiKey;
    private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta";

    public GeminiApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("GeminiApi");
        _logger = logger;
        _apiKey = configuration["AI:Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key not configured");
    }

    public async Task<string> GenerateWorkoutPlanAsync(decimal height, decimal weight, string? bodyType, string goal, string? photoBase64 = null)
    {
        try
        {
            var prompt = BuildWorkoutPrompt(height, weight, bodyType, goal);

            if (!string.IsNullOrEmpty(photoBase64))
            {
                return await GenerateWithVisionAsync(prompt, photoBase64);
            }
            else
            {
                return await GenerateTextAsync(prompt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workout planı oluşturulurken hata oluştu");
            throw;
        }
    }

    public async Task<string> GenerateDietPlanAsync(decimal height, decimal weight, string? bodyType, string goal, string? photoBase64 = null)
    {
        try
        {
            var prompt = BuildDietPrompt(height, weight, bodyType, goal);

            if (!string.IsNullOrEmpty(photoBase64))
            {
                return await GenerateWithVisionAsync(prompt, photoBase64);
            }
            else
            {
                return await GenerateTextAsync(prompt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diet planı oluşturulurken hata oluştu");
            throw;
        }
    }

    public async Task<string> AnalyzeBodyPhotoAsync(string photoBase64, decimal height, decimal weight, string goal)
    {
        try
        {
            var prompt = $@"Bu fotoğraftaki kişinin fiziksel durumunu analiz et.
Kişi Bilgileri:
- Boy: {height} cm
- Kilo: {weight} kg
- Hedef: {goal}

Lütfen şu bilgileri ver:
1. Vücut tipi analizi (ectomorph/mesomorph/endomorph)
2. Güncel fiziksel durum değerlendirmesi
3. Hedefine ulaşmak için öneriler
4. Tahmini hedefe ulaşma süresi

Türkçe olarak detaylı bir analiz yap.";

            return await GenerateWithVisionAsync(prompt, photoBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fotoğraf analizi sırasında hata oluştu");
            throw;
        }
    }

    private async Task<string> GenerateTextAsync(string prompt)
    {
        // Gemini 2.0 Flash modelini kullan - API key header'da
        var url = $"{API_BASE_URL}/models/gemini-2.0-flash-exp:generateContent";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,  // Daha tutarlı yanıtlar için düşürüldü
                topK = 20,
                topP = 0.8,
                maxOutputTokens = 2048  // Daha kısa yanıtlar için azaltıldı
            }
        };

        try
        {
            _logger.LogInformation("Gemini API'ye istek gönderiliyor: {Url}", url);
            
            // HttpRequestMessage ile custom header ekle
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            
            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini API yanıtı: {StatusCode}, İçerik uzunluğu: {Length}", 
                response.StatusCode, result.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API hatası: {Status} - {Content}", response.StatusCode, result);
                throw new HttpRequestException($"Gemini API hatası: {response.StatusCode} - {result}");
            }

            var jsonDoc = JsonDocument.Parse(result);

            var text = jsonDoc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Plan oluşturulamadı.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API çağrısında hata oluştu");
            throw;
        }
    }

    private async Task<string> GenerateWithVisionAsync(string prompt, string photoBase64)
    {
        // Gemini 2.0 Flash modelini kullan (vision desteği var)
        var url = $"{API_BASE_URL}/models/gemini-2.0-flash-exp:generateContent";

        // Base64'ten "data:image/...;base64," prefix'ini kaldır
        var base64Data = photoBase64;
        if (photoBase64.Contains(","))
        {
            base64Data = photoBase64.Split(',')[1];
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/jpeg",
                                data = base64Data
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,  // Daha tutarlı yanıtlar için düşürüldü
                topK = 20,
                topP = 0.8,
                maxOutputTokens = 2048  // Daha kısa yanıtlar için azaltıldı
            }
        };

        try
        {
            _logger.LogInformation("Gemini Vision API'ye istek gönderiliyor: {Url}", url);
            
            // HttpRequestMessage ile custom header ekle
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            
            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini Vision API yanıtı: {StatusCode}, İçerik uzunluğu: {Length}", 
                response.StatusCode, result.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Vision API hatası: {Status} - {Content}", response.StatusCode, result);
                throw new HttpRequestException($"Gemini Vision API hatası: {response.StatusCode} - {result}");
            }

            var jsonDoc = JsonDocument.Parse(result);

            var text = jsonDoc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Plan oluşturulamadı.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini Vision API çağrısında hata oluştu");
            throw;
        }
    }

    private string BuildWorkoutPrompt(decimal height, decimal weight, string? bodyType, string goal)
    {
        var bmi = weight / ((height / 100) * (height / 100));

        return $@"Sen bir fitness koçusun. Aşağıdaki bilgilere göre KISA ve ÖZ bir haftalık egzersiz planı oluştur.

Bilgiler:
- Boy: {height} cm, Kilo: {weight} kg, BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiş"}
- Hedef: {goal}

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

    private string BuildDietPrompt(decimal height, decimal weight, string? bodyType, string goal)
    {
        var bmi = weight / ((height / 100) * (height / 100));

        return $@"Sen bir beslenme uzmanısın. Aşağıdaki bilgilere göre KISA ve ÖZ bir haftalık diyet planı oluştur.

Bilgiler:
- Boy: {height} cm, Kilo: {weight} kg, BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiş"}
- Hedef: {goal}

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
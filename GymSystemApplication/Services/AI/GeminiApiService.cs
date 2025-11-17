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
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 8192
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
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 8192
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

        return $@"Bir fitness uzmanı olarak, aşağıdaki bilgilere göre detaylı bir egzersiz planı oluştur:

Kişisel Bilgiler:
- Boy: {height} cm
- Kilo: {weight} kg
- BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiş"}
- Hedef: {goal}

Lütfen şu başlıkları içeren detaylı bir plan hazırlayın:

1. GENEL DEĞERLENDİRME
   - Mevcut durum analizi
   - Hedef analizi

2. EGZERSİZ PROGRAMI (Haftalık)
   - Pazartesi
   - Salı
   - Çarşamba
   - Perşembe
   - Cuma
   - Cumartesi
   - Pazar
   
   Her gün için:
   - Hangi kas grupları çalışılacak
   - Yapılacak egzersizler (set x tekrar)
   - Süre
   - İpuçları

3. BESLENME ÖNERİLERİ
   - Günlük kalori ihtiyacı
   - Makro besin dağılımı
   - Örnek öğünler

4. HEDEF VE TAKİP
   - Beklenen ilerleme
   - Takip edilecek metrikler
   - Motivasyon tavsiyeleri

5. DİKKAT EDİLMESİ GEREKENLER
   - Güvenlik uyarıları
   - Yaygın hatalar

Plan en az 4 haftalık olsun ve detaylı açıklamalar içersin.
Türkçe olarak yaz.";
    }

    private string BuildDietPrompt(decimal height, decimal weight, string? bodyType, string goal)
    {
        var bmi = weight / ((height / 100) * (height / 100));

        return $@"Bir beslenme uzmanı olarak, aşağıdaki bilgilere göre detaylı bir diyet planı oluştur:

Kişisel Bilgiler:
- Boy: {height} cm
- Kilo: {weight} kg
- BMI: {bmi:F2}
- Vücut Tipi: {bodyType ?? "Belirtilmemiş"}
- Hedef: {goal}

Lütfen şu başlıkları içeren detaylı bir plan hazırlayın:

1. GENEL BESİN ANALİZİ
   - Günlük kalori ihtiyacı
   - Makro besin dağılımı (Protein/Karbonhidrat/Yağ)
   - Mikro besin önerileri

2. HAFTALIK DİYET PROGRAMI
   Her gün için detaylı öğün planı:
   - Sabah Kahvaltısı
   - Ara Öğün 1
   - Öğle Yemeği
   - Ara Öğün 2
   - Akşam Yemeği
   - Ara Öğün 3 (opsiyonel)
   
   Her öğün için:
   - Yiyecekler ve porsiyon
   - Kalori değeri
   - Makro besin değerleri

3. SUPPLEMENT ÖNERİLERİ
   - Önerilen takviyeler
   - Kullanım şekli ve zamanlaması

4. SIVI TÜKETİMİ
   - Günlük su ihtiyacı
   - Diğer içecek önerileri

5. ÖZEL NOTLAR
   - Kaçınılması gerekenler
   - Serbest günler
   - İpuçları

Plan 7 günlük detaylı menü içersin.
Türkçe olarak yaz.";
    }
}
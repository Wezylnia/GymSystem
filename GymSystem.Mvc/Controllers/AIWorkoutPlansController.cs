using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

[Authorize]
public class AIWorkoutPlansController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIWorkoutPlansController> _logger;

    public AIWorkoutPlansController(IHttpClientFactory httpClientFactory, ILogger<AIWorkoutPlansController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            // Member ise sadece kendi planlarını getir
            if (User.IsInRole("Member"))
            {
                var memberId = GetCurrentMemberId();

                if (memberId == null)
                {
                    ViewBag.ErrorMessage = "Member bilgisi bulunamadı.";
                    return View(new List<AIWorkoutPlanViewModel>());
                }

                var response = await client.GetAsync($"/api/aiworkoutplans/member/{memberId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var plans = JsonSerializer.Deserialize<List<AIWorkoutPlanViewModel>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<AIWorkoutPlanViewModel>();

                    return View(plans);
                }
                else
                {
                    ViewBag.ErrorMessage = "Planlar yüklenirken bir hata oluştu.";
                    return View(new List<AIWorkoutPlanViewModel>());
                }
            }
            // Admin veya GymOwner ise tüm planları getir
            else if (User.IsInRole("Admin") || User.IsInRole("GymOwner"))
            {
                var response = await client.GetAsync("/api/aiworkoutplans");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var plans = JsonSerializer.Deserialize<List<AIWorkoutPlanViewModel>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<AIWorkoutPlanViewModel>();

                    return View(plans);
                }
                else
                {
                    ViewBag.ErrorMessage = "Planlar yüklenirken bir hata oluştu.";
                    return View(new List<AIWorkoutPlanViewModel>());
                }
            }
            else
            {
                // Diğer roller için erişim engellendi
                return RedirectToAction("AccessDenied", "Account");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI planları listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<AIWorkoutPlanViewModel>());
        }
    }

    [Authorize(Roles = "Member")] // Sadece Member oluşturabilir
    public IActionResult Create()
    {
        var model = new CreateAIWorkoutPlanViewModel
        {
            MemberId = GetCurrentMemberId() ?? 0
        };

        ViewBag.BodyTypes = new SelectList(new[]
        {
            new { Value = "Ectomorph", Text = "Ectomorph (İnce, uzun)" },
            new { Value = "Mesomorph", Text = "Mesomorph (Atletik)" },
            new { Value = "Endomorph", Text = "Endomorph (Kaslı, dolgun)" }
        }, "Value", "Text");

        ViewBag.PlanTypes = new SelectList(new[]
        {
            new { Value = "Workout", Text = "Egzersiz Planı" },
            new { Value = "Diet", Text = "Diyet Planı" }
        }, "Value", "Text");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")] // Sadece Member oluşturabilir
    public async Task<IActionResult> Create(CreateAIWorkoutPlanViewModel model, IFormFile? photo)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var memberId = GetCurrentMemberId();

            if (memberId == null)
            {
                ModelState.AddModelError("", "Member bilgisi bulunamadı. Lütfen tekrar giriş yapın.");
                LoadDropdowns();
                return View(model);
            }

            // Fotoğraf varsa base64'e çevir
            string? photoBase64 = null;
            if (photo != null && photo.Length > 0)
            {
                // Dosya boyutu kontrolü (max 5MB)
                if (photo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Fotoğraf boyutu 5MB'dan küçük olmalıdır.");
                    LoadDropdowns();
                    return View(model);
                }

                using var memoryStream = new MemoryStream();
                await photo.CopyToAsync(memoryStream);
                var photoBytes = memoryStream.ToArray();
                photoBase64 = $"data:{photo.ContentType};base64,{Convert.ToBase64String(photoBytes)}";
                
                _logger.LogInformation("Fotoğraf yüklendi. Boyut: {Size} bytes", photo.Length);
            }

            var requestData = new
            {
                MemberId = memberId.Value,
                Height = model.Height,
                Weight = model.Weight,
                BodyType = model.BodyType,
                Goal = model.Goal,
                PhotoBase64 = photoBase64
            };

            var endpoint = model.PlanType == "Diet" ? "/api/aiworkoutplans/generate-diet" : "/api/aiworkoutplans/generate-workout";

            _logger.LogInformation("AI plan oluşturuluyor. Member ID: {MemberId}, Plan Type: {PlanType}", memberId.Value, model.PlanType);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(endpoint, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("AI planı başarıyla oluşturuldu. Member ID: {MemberId}", memberId.Value);
                TempData["SuccessMessage"] = "AI planı başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI planı oluşturulamadı. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                // Hata mesajını parse et
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    if (errorObj.TryGetProperty("error", out var errorMsg))
                    {
                        ModelState.AddModelError("", errorMsg.GetString() ?? "Plan oluşturulurken bir hata oluştu.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Plan oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Plan oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API'ye bağlanırken hata oluştu");
            ModelState.AddModelError("", "Sunucuya bağlanılamadı. Lütfen daha sonra tekrar deneyin.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI planı oluşturulurken beklenmeyen hata oluştu");
            ModelState.AddModelError("", "Beklenmeyen bir hata oluştu: " + ex.Message);
        }

        LoadDropdowns();
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync($"/api/aiworkoutplans/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var plan = JsonSerializer.Deserialize<AIWorkoutPlanViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (plan == null)
                {
                    return NotFound();
                }

                return View(plan);
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan detayı alınırken hata oluştu. ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member,Admin")] // Sadece Member ve Admin silebilir
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.DeleteAsync($"/api/aiworkoutplans/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Plan başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Plan silinirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan silinirken hata oluştu. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentMemberId()
    {
        var memberIdClaim = User.FindFirst("MemberId")?.Value;
        if (int.TryParse(memberIdClaim, out var memberId))
        {
            return memberId;
        }
        return null;
    }

    private void LoadDropdowns()
    {
        ViewBag.BodyTypes = new SelectList(new[]
        {
            new { Value = "Ectomorph", Text = "Ectomorph (İnce, uzun)" },
            new { Value = "Mesomorph", Text = "Mesomorph (Atletik)" },
            new { Value = "Endomorph", Text = "Endomorph (Kaslı, dolgun)" }
        }, "Value", "Text");

        ViewBag.PlanTypes = new SelectList(new[]
        {
            new { Value = "Workout", Text = "Egzersiz Planı" },
            new { Value = "Diet", Text = "Diyet Planı" }
        }, "Value", "Text");
    }
}
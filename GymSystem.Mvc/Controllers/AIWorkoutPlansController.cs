using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymSystem.Mvc.Controllers;

[Authorize]
public class AIWorkoutPlansController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<AIWorkoutPlansController> _logger;

    public AIWorkoutPlansController(ApiHelper apiHelper, IMapper mapper, ILogger<AIWorkoutPlansController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            List<ApiAIWorkoutPlanDto> apiPlans;

            // Member ise sadece kendi planlarını getir
            if (User.IsInRole("Member")) {
                var memberId = GetCurrentMemberId();
                if (memberId == null) {
                    ViewBag.ErrorMessage = "Member bilgisi bulunamadı.";
                    return View(new List<AIWorkoutPlanViewModel>());
                }

                apiPlans = await _apiHelper.GetListAsync<ApiAIWorkoutPlanDto>(
                    ApiEndpoints.AIWorkoutPlansByMember(memberId.Value));
            }
            // Admin veya GymOwner ise tüm planları getir
            else if (User.IsInRole("Admin") || User.IsInRole("GymOwner")) {
                apiPlans = await _apiHelper.GetListAsync<ApiAIWorkoutPlanDto>(ApiEndpoints.AIWorkoutPlans);
            }
            else {
                return RedirectToAction("AccessDenied", "Account");
            }

            // AutoMapper ile map et
            var plans = _mapper.Map<List<AIWorkoutPlanViewModel>>(apiPlans);
            return View(plans);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "AI planları listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<AIWorkoutPlanViewModel>());
        }
    }

    [Authorize(Roles = "Member")]
    public IActionResult Create() {
        var model = new CreateAIWorkoutPlanViewModel {
            MemberId = GetCurrentMemberId() ?? 0
        };

        LoadDropdowns();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create(CreateAIWorkoutPlanViewModel model, IFormFile? photo) {
        if (!ModelState.IsValid) {
            LoadDropdowns();
            return View(model);
        }

        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null) {
                ModelState.AddModelError("", "Member bilgisi bulunamadı. Lütfen tekrar giriş yapın.");
                LoadDropdowns();
                return View(model);
            }

            // Fotoğraf işleme
            string? photoBase64 = null;
            if (photo != null && photo.Length > 0) {
                if (photo.Length > 5 * 1024 * 1024) {
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

            var requestData = new {
                MemberId = memberId.Value,
                Height = model.Height,
                Weight = model.Weight,
                BodyType = model.BodyType,
                Goal = model.Goal,
                PhotoBase64 = photoBase64
            };

            var endpoint = model.PlanType == "Diet"
                ? "/api/aiworkoutplans/generate-diet"
                : "/api/aiworkoutplans/generate-workout";

            _logger.LogInformation("AI plan oluşturuluyor. Member ID: {MemberId}, Plan Type: {PlanType}",
                memberId.Value, model.PlanType);

            var (success, errorMessage) = await _apiHelper.PostAsync(endpoint, requestData);

            if (success) {
                _logger.LogInformation("AI planı başarıyla oluşturuldu. Member ID: {MemberId}", memberId.Value);
                TempData["SuccessMessage"] = "AI planı başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Plan oluşturulurken bir hata oluştu. {errorMessage}");
            }
        }
        catch (HttpRequestException ex) {
            _logger.LogError(ex, "API'ye bağlanırken hata oluştu");
            ModelState.AddModelError("", "Sunucuya bağlanılamadı. Lütfen daha sonra tekrar deneyin.");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "AI planı oluşturulurken beklenmeyen hata oluştu");
            ModelState.AddModelError("", "Beklenmeyen bir hata oluştu: " + ex.Message);
        }

        LoadDropdowns();
        return View(model);
    }

    public async Task<IActionResult> Details(int id) {
        try {
            var plan = await _apiHelper.GetAsync<ApiAIWorkoutPlanDto>(ApiEndpoints.AIWorkoutPlanById(id));

            if (plan == null)
                return NotFound();

            var viewModel = _mapper.Map<AIWorkoutPlanViewModel>(plan);
            return View(viewModel);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Plan detayı alınırken hata oluştu. ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> Delete(int id) {
        try {
            var (success, errorMessage) = await _apiHelper.DeleteAsync(ApiEndpoints.AIWorkoutPlanById(id));

            if (success) {
                TempData["SuccessMessage"] = "Plan başarıyla silindi.";
            }
            else {
                TempData["ErrorMessage"] = $"Plan silinirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Plan silinirken hata oluştu. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    #region Helpers

    private int? GetCurrentMemberId() {
        var memberIdClaim = User.FindFirst("MemberId")?.Value;
        return int.TryParse(memberIdClaim, out var memberId) ? memberId : null;
    }

    private void LoadDropdowns() {
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

    #endregion
}
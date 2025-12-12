using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class TrainersController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<TrainersController> _logger;

    public TrainersController(ApiHelper apiHelper, IMapper mapper, ILogger<TrainersController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            var apiTrainers = await _apiHelper.GetListAsync<ApiTrainerDto>(ApiEndpoints.Trainers);

            // AutoMapper ile ViewModel'e map et
            var trainers = _mapper.Map<List<TrainerViewModel>>(apiTrainers);

            // GymOwner ise sadece kendi salonunun antrenörlerini göster
            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId)) {
                    trainers = trainers.Where(t => t.GymLocationId == locationId).ToList();
                }
            }

            return View(trainers);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenörler listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<TrainerViewModel>());
        }
    }

    public async Task<IActionResult> Create() {
        await LoadGymLocations();
        await LoadAllServices();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerViewModel model) {
        // GymOwner için salon otomatik set
        if (User.IsInRole("GymOwner")) {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (int.TryParse(gymLocationId, out var locationId)) {
                model.GymLocationId = locationId;
            }
        }

        if (!ModelState.IsValid) {
            await LoadGymLocations();
            await LoadServicesByGymLocation(model.GymLocationId);
            return View(model);
        }

        try {
            var dto = _mapper.Map<ApiTrainerDto>(model);
            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.Trainers, dto);

            if (success) {
                TempData["SuccessMessage"] = "Antrenör başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Antrenör eklenirken bir hata oluştu. {errorMessage}");
                await LoadGymLocations();
                await LoadServicesByGymLocation(model.GymLocationId);
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadGymLocations();
            await LoadServicesByGymLocation(model.GymLocationId);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id) {
        try {
            var trainer = await _apiHelper.GetAsync<ApiTrainerDto>(ApiEndpoints.TrainerById(id));

            if (trainer == null) {
                TempData["ErrorMessage"] = "Antrenör bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // GymOwner yetki kontrolü
            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId) && trainer.GymLocationId != locationId) {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var viewModel = _mapper.Map<TrainerViewModel>(trainer);
            await LoadGymLocations();
            await LoadServicesByGymLocation(trainer.GymLocationId);
            return View(viewModel);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör detayı alınırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainerViewModel model) {
        if (id != model.Id) {
            return BadRequest();
        }

        // GymOwner yetki kontrolü
        if (User.IsInRole("GymOwner")) {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (int.TryParse(gymLocationId, out var locationId) && model.GymLocationId != locationId) {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        if (!ModelState.IsValid) {
            await LoadGymLocations();
            await LoadServicesByGymLocation(model.GymLocationId);
            return View(model);
        }

        try {
            var dto = _mapper.Map<ApiTrainerDto>(model);
            var (success, errorMessage) = await _apiHelper.PutAsync(ApiEndpoints.TrainerById(id), dto);

            if (success) {
                TempData["SuccessMessage"] = "Antrenör başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Antrenör güncellenirken bir hata oluştu. {errorMessage}");
                await LoadGymLocations();
                await LoadServicesByGymLocation(model.GymLocationId);
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör güncellenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadGymLocations();
            await LoadServicesByGymLocation(model.GymLocationId);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) {
        try {
            var (success, errorMessage) = await _apiHelper.DeleteAsync(ApiEndpoints.TrainerById(id));

            if (success) {
                TempData["SuccessMessage"] = "Antrenör başarıyla silindi!";
            }
            else {
                TempData["ErrorMessage"] = $"Antrenör silinirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör silinirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// AJAX: Salona göre hizmetleri getir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetServicesByGymLocation(int gymLocationId) {
        try {
            var services = await _apiHelper.GetListAsync<ApiServiceDto>(ApiEndpoints.Services);
            var filteredServices = services.Where(s => s.GymLocationId == gymLocationId).Select(s => new {
                id = s.Id,
                name = s.Name
            }).ToList();
            return Json(filteredServices);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler yüklenirken hata oluştu");
            return Json(new List<object>());
        }
    }

    private async Task LoadGymLocations() {
        try {
            var gyms = await _apiHelper.GetListAsync<GymLocationViewModel>(ApiEndpoints.GymLocations);

            // GymOwner ise sadece kendi salonunu göster
            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId)) {
                    gyms = gyms.Where(g => g.Id == locationId).ToList();
                }
            }

            ViewBag.GymLocations = new SelectList(gyms, "Id", "Name");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonları yüklenirken hata oluştu");
            ViewBag.GymLocations = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }

    private async Task LoadAllServices() {
        try {
            var services = await _apiHelper.GetListAsync<ApiServiceDto>(ApiEndpoints.Services);
            ViewBag.AllServices = services;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler yüklenirken hata oluştu");
            ViewBag.AllServices = new List<ApiServiceDto>();
        }
    }

    private async Task LoadServicesByGymLocation(int gymLocationId) {
        try {
            var services = await _apiHelper.GetListAsync<ApiServiceDto>(ApiEndpoints.Services);
            var filteredServices = services.Where(s => s.GymLocationId == gymLocationId).ToList();
            ViewBag.GymServices = filteredServices;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler yüklenirken hata oluştu");
            ViewBag.GymServices = new List<ApiServiceDto>();
        }
    }
}
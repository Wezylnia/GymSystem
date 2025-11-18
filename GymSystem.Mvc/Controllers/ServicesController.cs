using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class ServicesController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(ApiHelper apiHelper, IMapper mapper, ILogger<ServicesController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            var apiServices = await _apiHelper.GetListAsync<ApiServiceDto>(ApiEndpoints.Services);

            var services = _mapper.Map<List<ServiceViewModel>>(apiServices);

            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId)) {
                    services = services.Where(s => s.GymLocationId == locationId).ToList();
                }
            }

            return View(services);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<ServiceViewModel>());
        }
    }

    public async Task<IActionResult> Create() {
        await LoadGymLocations();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceViewModel model) {
        if (User.IsInRole("GymOwner")) {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (int.TryParse(gymLocationId, out var locationId)) {
                model.GymLocationId = locationId;
            }
        }

        if (!ModelState.IsValid) {
            await LoadGymLocations();
            return View(model);
        }

        try {
            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.Services, model);

            if (success) {
                TempData["SuccessMessage"] = "Hizmet başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Hizmet eklenirken bir hata oluştu. {errorMessage}");
                await LoadGymLocations();
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadGymLocations();
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id) {
        try {
            var service = await _apiHelper.GetAsync<ApiServiceDto>(ApiEndpoints.ServiceById(id));

            if (service == null) {
                TempData["ErrorMessage"] = "Hizmet bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId) && service.GymLocationId != locationId) {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var viewModel = _mapper.Map<ServiceViewModel>(service);
            await LoadGymLocations();
            return View(viewModel);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet detayı alınırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceViewModel model) {
        if (id != model.Id) {
            return BadRequest();
        }

        if (User.IsInRole("GymOwner")) {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (int.TryParse(gymLocationId, out var locationId) && model.GymLocationId != locationId) {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        if (!ModelState.IsValid) {
            await LoadGymLocations();
            return View(model);
        }

        try {
            var (success, errorMessage) = await _apiHelper.PutAsync(ApiEndpoints.ServiceById(id), model);

            if (success) {
                TempData["SuccessMessage"] = "Hizmet başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Hizmet güncellenirken bir hata oluştu. {errorMessage}");
                await LoadGymLocations();
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet güncellenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadGymLocations();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) {
        try {
            var (success, errorMessage) = await _apiHelper.DeleteAsync(ApiEndpoints.ServiceById(id));

            if (success) {
                TempData["SuccessMessage"] = "Hizmet başarıyla silindi!";
            }
            else {
                TempData["ErrorMessage"] = $"Hizmet silinirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet silinirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadGymLocations() {
        try {
            var gyms = await _apiHelper.GetListAsync<GymLocationViewModel>(ApiEndpoints.GymLocations);

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
}
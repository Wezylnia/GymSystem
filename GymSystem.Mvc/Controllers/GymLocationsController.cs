using GymSystem.Domain.Entities;
using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class GymLocationsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GymLocationsController> _logger;

    public GymLocationsController(
        IHttpClientFactory httpClientFactory,
        ILogger<GymLocationsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/gymlocations");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var gyms = JsonSerializer.Deserialize<List<GymLocationViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<GymLocationViewModel>();

                // GymOwner ise sadece kendi salonunu göster
                if (User.IsInRole("GymOwner"))
                {
                    var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                    if (int.TryParse(gymLocationId, out var locationId))
                    {
                        gyms = gyms.Where(g => g.Id == locationId).ToList();
                    }
                }

                return View(gyms);
            }
            else
            {
                ViewBag.ErrorMessage = "Spor salonları yüklenirken bir hata oluştu.";
                return View(new List<GymLocationViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spor salonları listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<GymLocationViewModel>());
        }
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(GymLocationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/gymlocations", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Spor salonu başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Spor salonu eklenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                ModelState.AddModelError("", "Spor salonu eklenirken bir hata oluştu.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spor salonu eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        // GymOwner yetki kontrolü
        if (User.IsInRole("GymOwner"))
        {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (!int.TryParse(gymLocationId, out var locationId) || locationId != id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync($"/api/gymlocations/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var gym = JsonSerializer.Deserialize<GymLocationViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return View(gym);
            }
            else
            {
                TempData["ErrorMessage"] = "Spor salonu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spor salonu detayı alınırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GymLocationViewModel model)
    {
        // GymOwner yetki kontrolü
        if (User.IsInRole("GymOwner"))
        {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (!int.TryParse(gymLocationId, out var locationId) || locationId != id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"/api/gymlocations/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Spor salonu başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Spor salonu güncellenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                ModelState.AddModelError("", "Spor salonu güncellenirken bir hata oluştu.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spor salonu güncellenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.DeleteAsync($"/api/gymlocations/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Spor salonu başarıyla silindi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Spor salonu silinirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spor salonu silinirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

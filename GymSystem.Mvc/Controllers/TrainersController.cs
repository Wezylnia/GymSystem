using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class TrainersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TrainersController> _logger;

    public TrainersController(IHttpClientFactory httpClientFactory, ILogger<TrainersController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/trainers");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var trainers = JsonSerializer.Deserialize<List<TrainerViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<TrainerViewModel>();

                // GymOwner ise sadece kendi salonunun antrenörlerini göster
                if (User.IsInRole("GymOwner"))
                {
                    var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                    if (int.TryParse(gymLocationId, out var locationId))
                    {
                        trainers = trainers.Where(t => t.GymLocationId == locationId).ToList();
                    }
                }

                return View(trainers);
            }
            else
            {
                ViewBag.ErrorMessage = "Antrenörler yüklenirken bir hata oluştu.";
                return View(new List<TrainerViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenörler listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<TrainerViewModel>());
        }
    }

    public async Task<IActionResult> Create()
    {
        await LoadGymLocations();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerViewModel model)
    {
        // GymOwner için salon otomatik set
        if (User.IsInRole("GymOwner"))
        {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (int.TryParse(gymLocationId, out var locationId))
            {
                model.GymLocationId = locationId;
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadGymLocations();
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("/api/trainers", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Antrenör başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Antrenör eklenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                ModelState.AddModelError("", "Antrenör eklenirken bir hata oluştu.");
                await LoadGymLocations();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadGymLocations();
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync($"/api/trainers/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var trainer = JsonSerializer.Deserialize<TrainerViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // GymOwner yetki kontrolü
                if (User.IsInRole("GymOwner"))
                {
                    var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                    if (trainer != null && int.TryParse(gymLocationId, out var locationId) && trainer.GymLocationId != locationId)
                    {
                        return RedirectToAction("AccessDenied", "Account");
                    }
                }

                await LoadGymLocations();
                return View(trainer);
            }
            else
            {
                TempData["ErrorMessage"] = "Antrenör bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör detayı alınırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainerViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        // GymOwner yetki kontrolü
        if (User.IsInRole("GymOwner"))
        {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (int.TryParse(gymLocationId, out var locationId) && model.GymLocationId != locationId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadGymLocations();
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync($"/api/trainers/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Antrenör başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Antrenör güncellenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                ModelState.AddModelError("", "Antrenör güncellenirken bir hata oluştu.");
                await LoadGymLocations();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör güncellenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadGymLocations();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.DeleteAsync($"/api/trainers/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Antrenör başarıyla silindi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Antrenör silinirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör silinirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadGymLocations()
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

                ViewBag.GymLocations = new SelectList(gyms, "Id", "Name");
            }
            else
            {
                ViewBag.GymLocations = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spor salonları yüklenirken hata oluştu");
            ViewBag.GymLocations = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}

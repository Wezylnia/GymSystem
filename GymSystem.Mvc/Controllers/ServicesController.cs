using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

public class ServicesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IHttpClientFactory httpClientFactory, ILogger<ServicesController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/services");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var services = JsonSerializer.Deserialize<List<ServiceViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return View(services ?? new List<ServiceViewModel>());
            }
            else
            {
                ViewBag.ErrorMessage = "Hizmetler yüklenirken bir hata oluştu.";
                return View(new List<ServiceViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hizmetler listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<ServiceViewModel>());
        }
    }

    public async Task<IActionResult> Create()
    {
        await LoadGymLocations();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceViewModel model)
    {
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
            
            var response = await client.PostAsync("/api/services", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Hizmet başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Hizmet eklenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                ModelState.AddModelError("", "Hizmet eklenirken bir hata oluştu.");
                await LoadGymLocations();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hizmet eklenirken hata oluştu");
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
            var response = await client.GetAsync($"/api/services/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var service = JsonSerializer.Deserialize<ServiceViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                await LoadGymLocations();
                return View(service);
            }
            else
            {
                TempData["ErrorMessage"] = "Hizmet bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hizmet detayı alınırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
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
            
            var response = await client.PutAsync($"/api/services/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Hizmet başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Hizmet güncellenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                ModelState.AddModelError("", "Hizmet güncellenirken bir hata oluştu.");
                await LoadGymLocations();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hizmet güncellenirken hata oluştu");
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
            var response = await client.DeleteAsync($"/api/services/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Hizmet başarıyla silindi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Hizmet silinirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hizmet silinirken hata oluştu");
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
                });

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

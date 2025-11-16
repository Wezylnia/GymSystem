using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

public class AppointmentsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IHttpClientFactory httpClientFactory, ILogger<AppointmentsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/appointments");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var appointments = JsonSerializer.Deserialize<List<AppointmentViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return View(appointments ?? new List<AppointmentViewModel>());
            }
            else
            {
                ViewBag.ErrorMessage = "Randevular yüklenirken bir hata oluştu.";
                return View(new List<AppointmentViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevular listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<AppointmentViewModel>());
        }
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            // Service bilgisini al (süre ve fiyat için)
            var serviceResponse = await client.GetAsync($"/api/services/{model.ServiceId}");
            if (!serviceResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Hizmet bilgisi alınamadı.");
                await LoadDropdowns();
                return View(model);
            }

            var serviceContent = await serviceResponse.Content.ReadAsStringAsync();
            var service = JsonSerializer.Deserialize<ServiceViewModel>(serviceContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Appointment nesnesi oluştur
            var appointment = new
            {
                MemberId = model.MemberId,
                TrainerId = model.TrainerId,
                ServiceId = model.ServiceId,
                AppointmentDate = model.AppointmentDate.Add(model.AppointmentTime),
                DurationMinutes = service?.DurationMinutes ?? 60,
                Price = service?.Price ?? 0,
                Notes = model.Notes
            };

            var json = JsonSerializer.Serialize(appointment);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("/api/appointments", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu! Onay bekliyor.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Randevu oluşturulurken hata oluştu. Status Code: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                // API'den gelen hata mesajını parse et
                try
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent);
                    if (errorObj != null && errorObj.ContainsKey("errorMessage"))
                    {
                        ModelState.AddModelError("", errorObj["errorMessage"].ToString() ?? "Randevu oluşturulamadı.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Randevu oluşturulurken bir hata oluştu.");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Randevu oluşturulurken bir hata oluştu.");
                }
                
                await LoadDropdowns();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadDropdowns();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.PutAsync($"/api/appointments/{id}/confirm", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Randevu onaylandı!";
            }
            else
            {
                TempData["ErrorMessage"] = "Randevu onaylanırken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu onaylanırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            var json = JsonSerializer.Serialize(reason ?? "");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync($"/api/appointments/{id}/cancel", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Randevu iptal edildi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Randevu iptal edilirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu iptal edilirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetServicesByGym(int gymLocationId)
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

                var filteredServices = services?.Where(s => s.GymLocationId == gymLocationId).ToList() ?? new List<ServiceViewModel>();
                return Json(filteredServices);
            }

            return Json(new List<ServiceViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hizmetler yüklenirken hata oluştu");
            return Json(new List<ServiceViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTrainersByService(int serviceId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            // Önce service'i al
            var serviceResponse = await client.GetAsync($"/api/services/{serviceId}");
            if (!serviceResponse.IsSuccessStatusCode)
            {
                return Json(new List<TrainerViewModel>());
            }

            // Tüm trainers'ı al ve filtrele (gerçek projede TrainerSpecialty üzerinden yapılmalı)
            var trainersResponse = await client.GetAsync("/api/trainers");
            if (trainersResponse.IsSuccessStatusCode)
            {
                var content = await trainersResponse.Content.ReadAsStringAsync();
                var trainers = JsonSerializer.Deserialize<List<TrainerViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Json(trainers ?? new List<TrainerViewModel>());
            }

            return Json(new List<TrainerViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenörler yüklenirken hata oluştu");
            return Json(new List<TrainerViewModel>());
        }
    }

    private async Task LoadDropdowns()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");

            // Members
            var membersResponse = await client.GetAsync("/api/members");
            if (membersResponse.IsSuccessStatusCode)
            {
                var content = await membersResponse.Content.ReadAsStringAsync();
                var members = JsonSerializer.Deserialize<List<MemberViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                ViewBag.Members = new SelectList(members, "Id", "FirstName");
            }
            else
            {
                ViewBag.Members = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            // GymLocations
            var gymsResponse = await client.GetAsync("/api/gymlocations");
            if (gymsResponse.IsSuccessStatusCode)
            {
                var content = await gymsResponse.Content.ReadAsStringAsync();
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

            ViewBag.Services = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.Trainers = new SelectList(Enumerable.Empty<SelectListItem>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dropdown verileri yüklenirken hata oluştu");
            ViewBag.Members = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.GymLocations = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.Services = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.Trainers = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}

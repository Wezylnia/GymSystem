using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymSystem.Mvc.Controllers;

[Authorize]
public class AppointmentsController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(ApiHelper apiHelper, IMapper mapper, ILogger<AppointmentsController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            List<ApiAppointmentDto> apiAppointments;

            // Member ise member-specific endpoint kullan
            if (User.IsInRole("Member")) {
                var memberIdClaim = User.FindFirst("MemberId")?.Value;
                if (int.TryParse(memberIdClaim, out var memberId)) {
                    apiAppointments = await _apiHelper.GetListAsync<ApiAppointmentDto>($"/api/appointments/member/{memberId}");
                }
                else {
                    _logger.LogWarning("Member ID claim not found for user {User}", User.Identity?.Name);
                    ViewBag.ErrorMessage = "Üye bilgisi bulunamadı.";
                    return View(new List<AppointmentViewModel>());
                }
            }
            else {
                // Admin ve GymOwner için GetAll kullan
                apiAppointments = await _apiHelper.GetListAsync<ApiAppointmentDto>(ApiEndpoints.Appointments);
            }

            // AutoMapper ile map et
            var appointments = _mapper.Map<List<AppointmentViewModel>>(apiAppointments);

            return View(appointments);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevular listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<AppointmentViewModel>());
        }
    }

    public async Task<IActionResult> Create() {
        await LoadDropdowns();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAppointmentViewModel model) {
        // Member ise kendi ID'sini otomatik set et
        if (User.IsInRole("Member")) {
            var memberIdClaim = User.FindFirst("MemberId")?.Value;
            if (int.TryParse(memberIdClaim, out var memberId)) {
                model.MemberId = memberId;
            }
        }

        if (!ModelState.IsValid) {
            await LoadDropdowns();
            return View(model);
        }

        try {
            // Service bilgisini al
            var service = await _apiHelper.GetAsync<ApiServiceDto>(ApiEndpoints.ServiceById(model.ServiceId));
            if (service == null) {
                ModelState.AddModelError("", "Hizmet bilgisi alınamadı.");
                await LoadDropdowns();
                return View(model);
            }

            var appointment = new {
                MemberId = model.MemberId,
                TrainerId = model.TrainerId,
                ServiceId = model.ServiceId,
                AppointmentDate = model.AppointmentDate.Add(model.AppointmentTime),
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                Notes = model.Notes
            };

            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.Appointments, appointment);

            if (success) {
                TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu! Onay bekliyor.";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Randevu oluşturulurken bir hata oluştu. {errorMessage}");
                await LoadDropdowns();
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            await LoadDropdowns();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOrGymOwner")]
    public async Task<IActionResult> Confirm(int id) {
        try {
            var rawResponse = await _apiHelper.GetRawAsync($"/api/appointments/{id}/confirm");
            // PUT metodu için raw kullanıyoruz
            var (success, errorMessage) = await _apiHelper.PutAsync($"/api/appointments/{id}/confirm", new { });

            if (success) {
                TempData["SuccessMessage"] = "Randevu onaylandı!";
            }
            else {
                TempData["ErrorMessage"] = $"Randevu onaylanırken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu onaylanırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason) {
        try {
            var (success, errorMessage) = await _apiHelper.PutAsync($"/api/appointments/{id}/cancel", reason ?? "");

            if (success) {
                TempData["SuccessMessage"] = "Randevu iptal edildi!";
            }
            else {
                TempData["ErrorMessage"] = $"Randevu iptal edilirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu iptal edilirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetServicesByGym(int gymLocationId) {
        try {
            var services = await _apiHelper.GetListAsync<ApiServiceDto>(ApiEndpoints.Services);
            var filtered = services.Where(s => s.GymLocationId == gymLocationId).ToList();
            return Json(filtered);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler yüklenirken hata oluştu");
            return Json(new List<ApiServiceDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTrainersByService(int serviceId) {
        try {
            // Önce hizmet bilgisini al (hangi salona ait olduğunu öğrenmek için)
            var service = await _apiHelper.GetAsync<ApiServiceDto>(ApiEndpoints.ServiceById(serviceId));
            
            if (service == null) {
                _logger.LogWarning("Service bulunamadı. ServiceId: {ServiceId}", serviceId);
                return Json(new List<ApiTrainerDto>());
            }

            // Tüm antrenörleri al
            var allTrainers = await _apiHelper.GetListAsync<ApiTrainerDto>(ApiEndpoints.Trainers);
            
            // Sadece bu hizmetin salonundaki aktif antrenörleri filtrele
            var filtered = allTrainers
                .Where(t => t.GymLocationId == service.GymLocationId && t.IsActive)
                .ToList();

            _logger.LogInformation("ServiceId: {ServiceId}, GymLocationId: {GymLocationId}, Antrenör sayısı: {Count}", 
                serviceId, service.GymLocationId, filtered.Count);

            return Json(filtered);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenörler yüklenirken hata oluştu. ServiceId: {ServiceId}", serviceId);
            return Json(new List<ApiTrainerDto>());
        }
    }

    #region Helpers

    private async Task LoadDropdowns() {
        try {
            // Members
            if (User.IsInRole("Member")) {
                // Member için dropdown gerekmez, hidden input kullanılacak
                var memberIdClaim = User.FindFirst("MemberId")?.Value;
                if (int.TryParse(memberIdClaim, out var memberId)) {
                    var memberName = $"{User.Identity!.Name}";
                    ViewBag.CurrentMemberId = memberId;
                    ViewBag.CurrentMemberName = memberName;
                }
            }
            else if (User.IsInRole("Admin")) {
                // Admin tüm üyeleri görebilir
                var members = await _apiHelper.GetListAsync<ApiMemberDto>(ApiEndpoints.Members);
                var memberList = members.Select(m => new {
                    Id = m.Id,
                    FullName = $"{m.FirstName} {m.LastName} ({m.Email})"
                }).ToList();
                ViewBag.Members = new SelectList(memberList, "Id", "FullName");
            }
            else if (User.IsInRole("GymOwner")) {
                // GymOwner sadece kendi salonunun aktif üyelerini görebilir
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId)) {
                    var allMembers = await _apiHelper.GetListAsync<ApiMemberDto>(ApiEndpoints.Members);
                    // Aktif üyelik olan ve bu salona ait üyeleri filtrele
                    var activeMembers = allMembers
                        .Where(m => m.IsActive && 
                                   m.MembershipEndDate.HasValue && 
                                   m.MembershipEndDate.Value >= DateTime.Now &&
                                   m.CurrentGymLocationId == locationId)
                        .Select(m => new {
                            Id = m.Id,
                            FullName = $"{m.FirstName} {m.LastName} ({m.Email})"
                        })
                        .ToList();
                    ViewBag.Members = new SelectList(activeMembers, "Id", "FullName");
                }
            }

            // GymLocations
            var gyms = await _apiHelper.GetListAsync<ApiGymLocationFullDto>(ApiEndpoints.GymLocations);

            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId)) {
                    gyms = gyms.Where(g => g.Id == locationId).ToList();
                }
            }

            ViewBag.GymLocations = new SelectList(gyms, "Id", "Name");
            ViewBag.Services = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.Trainers = new SelectList(Enumerable.Empty<SelectListItem>());
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Dropdown verileri yüklenirken hata oluştu");
            ViewBag.Members = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.GymLocations = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.Services = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.Trainers = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }

    #endregion
}
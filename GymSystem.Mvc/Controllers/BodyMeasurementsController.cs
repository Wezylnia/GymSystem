using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Mvc.Controllers;

[Authorize(Roles = "Member")]
public class BodyMeasurementsController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<BodyMeasurementsController> _logger;

    public BodyMeasurementsController(ApiHelper apiHelper, IMapper mapper, ILogger<BodyMeasurementsController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null) {
                ViewBag.ErrorMessage = "Üye bilgisi bulunamadý.";
                return View(new BodyMeasurementListViewModel());
            }

            var apiMeasurements = await _apiHelper.GetListAsync<ApiBodyMeasurementDto>(
                ApiEndpoints.BodyMeasurementsByMember(memberId.Value));

            var measurements = _mapper.Map<List<BodyMeasurementViewModel>>(apiMeasurements);

            var viewModel = new BodyMeasurementListViewModel {
                Measurements = measurements,
                MemberId = memberId.Value,
                TotalMeasurements = measurements.Count
            };

            // Ýstatistikleri hesapla
            if (measurements.Any()) {
                var latest = measurements.OrderByDescending(m => m.MeasurementDate).First();
                var oldest = measurements.OrderBy(m => m.MeasurementDate).First();

                viewModel.CurrentWeight = latest.Weight;
                viewModel.CurrentHeight = latest.Height;
                viewModel.TotalWeightChange = latest.Weight - oldest.Weight;
                viewModel.TotalHeightChange = latest.Height - oldest.Height;
            }

            return View(viewModel);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçümler listesi alýnýrken hata oluþtu");
            ViewBag.ErrorMessage = "Bir hata oluþtu: " + ex.Message;
            return View(new BodyMeasurementListViewModel());
        }
    }

    public IActionResult Create() {
        var model = new BodyMeasurementViewModel {
            MemberId = GetCurrentMemberId() ?? 0,
            MeasurementDate = DateTime.Today,
            Height = 170, // Varsayýlan deðerler
            Weight = 70
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BodyMeasurementViewModel model) {
        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null) {
                ModelState.AddModelError("", "Üye bilgisi bulunamadý.");
                return View(model);
            }

            model.MemberId = memberId.Value;

            if (!ModelState.IsValid)
                return View(model);

            var requestData = new {
                MemberId = model.MemberId,
                MeasurementDate = model.MeasurementDate,
                Height = model.Height,
                Weight = model.Weight,
                Note = model.Note
            };

            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.BodyMeasurements, requestData);

            if (success) {
                TempData["SuccessMessage"] = "Ölçüm baþarýyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Ölçüm eklenirken hata oluþtu. {errorMessage}");
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm eklenirken hata oluþtu");
            ModelState.AddModelError("", "Beklenmeyen bir hata oluþtu: " + ex.Message);
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id) {
        try {
            var measurement = await _apiHelper.GetAsync<ApiBodyMeasurementDto>(ApiEndpoints.BodyMeasurementById(id));

            if (measurement == null)
                return NotFound();

            // Sadece kendi ölçümünü düzenleyebilsin
            var memberId = GetCurrentMemberId();
            if (measurement.MemberId != memberId)
                return Forbid();

            var viewModel = _mapper.Map<BodyMeasurementViewModel>(measurement);
            return View(viewModel);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm detayý alýnýrken hata oluþtu. ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BodyMeasurementViewModel model) {
        try {
            if (id != model.Id)
                return BadRequest();

            // Sadece kendi ölçümünü düzenleyebilsin
            var memberId = GetCurrentMemberId();
            if (model.MemberId != memberId)
                return Forbid();

            if (!ModelState.IsValid)
                return View(model);

            var requestData = new {
                Id = model.Id,
                MemberId = model.MemberId,
                MeasurementDate = model.MeasurementDate,
                Height = model.Height,
                Weight = model.Weight,
                Note = model.Note
            };

            var (success, errorMessage) = await _apiHelper.PutAsync(ApiEndpoints.BodyMeasurementById(id), requestData);

            if (success) {
                TempData["SuccessMessage"] = "Ölçüm baþarýyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Ölçüm güncellenirken hata oluþtu. {errorMessage}");
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm güncellenirken hata oluþtu. ID: {Id}", id);
            ModelState.AddModelError("", "Beklenmeyen bir hata oluþtu: " + ex.Message);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) {
        try {
            // Önce ölçümü kontrol et
            var measurement = await _apiHelper.GetAsync<ApiBodyMeasurementDto>(ApiEndpoints.BodyMeasurementById(id));
            
            if (measurement == null)
                return NotFound();

            // Sadece kendi ölçümünü silebilsin
            var memberId = GetCurrentMemberId();
            if (measurement.MemberId != memberId)
                return Forbid();

            var (success, errorMessage) = await _apiHelper.DeleteAsync(ApiEndpoints.BodyMeasurementById(id));

            if (success) {
                TempData["SuccessMessage"] = "Ölçüm baþarýyla silindi!";
            }
            else {
                TempData["ErrorMessage"] = $"Ölçüm silinirken hata oluþtu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Ölçüm silinirken hata oluþtu. ID: {Id}", id);
            TempData["ErrorMessage"] = "Beklenmeyen bir hata oluþtu.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Grafik verileri için JSON endpoint
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetChartData() {
        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null)
                return Json(new { success = false, message = "Üye bilgisi bulunamadý." });

            var measurements = await _apiHelper.GetListAsync<ApiBodyMeasurementDto>(
                ApiEndpoints.BodyMeasurementsChart(memberId.Value));

            // Tarihe göre artan sýralý
            var orderedMeasurements = measurements.OrderBy(m => m.MeasurementDate).ToList();

            var chartData = new {
                success = true,
                labels = orderedMeasurements.Select(m => m.MeasurementDate.ToString("dd/MM/yyyy")).ToList(),
                weightData = orderedMeasurements.Select(m => m.Weight).ToList(),
                heightData = orderedMeasurements.Select(m => m.Height).ToList(),
                bmiData = orderedMeasurements.Select(m => m.BMI).ToList()
            };

            return Json(chartData);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Grafik verileri alýnýrken hata oluþtu");
            return Json(new { success = false, message = "Veriler alýnýrken hata oluþtu." });
        }
    }

    private int? GetCurrentMemberId() {
        var memberIdClaim = User.FindFirst("MemberId")?.Value;
        if (int.TryParse(memberIdClaim, out int memberId))
            return memberId;
        return null;
    }
}

using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

[Authorize(Roles = "Admin,GymOwner")]
public class DashboardController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApiHelper apiHelper, ILogger<DashboardController> logger) {
        _apiHelper = apiHelper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            var dashboard = new DashboardViewModel();

            // Get gym location ID if GymOwner
            int? gymLocationId = null;
            if (User.IsInRole("GymOwner")) {
                var gymLocationIdClaim = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationIdClaim, out var locationId)) {
                    gymLocationId = locationId;
                }
            }

            var queryParam = gymLocationId.HasValue ? $"?gymLocationId={gymLocationId}" : "";

            // Fetch Dashboard Stats
            dashboard.Stats = await _apiHelper.GetAsync<DashboardStatsViewModel>(
                $"/api/reports/gym-owner-dashboard{queryParam}") ?? new DashboardStatsViewModel();

            // Fetch Membership Statistics
            dashboard.MembershipStats = await _apiHelper.GetAsync<MembershipStatisticsViewModel>(
                $"/api/reports/membership-statistics{queryParam}") ?? new MembershipStatisticsViewModel();

            // Fetch Revenue Trend
            var revenueTrendResponse = await _apiHelper.GetRawAsync($"/api/reports/revenue-trend{queryParam}");
            if (revenueTrendResponse.IsSuccessStatusCode) {
                var revenueContent = await revenueTrendResponse.Content.ReadAsStringAsync();
                var revenueData = JsonSerializer.Deserialize<JsonElement>(revenueContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (revenueData.TryGetProperty("trend", out var trendElement)) {
                    dashboard.RevenueTrend = JsonSerializer.Deserialize<List<RevenueTrendItem>>(
                        trendElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RevenueTrendItem>();
                }
            }

            // Fetch Member Growth Trend
            var memberGrowthResponse = await _apiHelper.GetRawAsync($"/api/reports/member-growth-trend{queryParam}");
            if (memberGrowthResponse.IsSuccessStatusCode) {
                var memberGrowthContent = await memberGrowthResponse.Content.ReadAsStringAsync();
                var memberGrowthData = JsonSerializer.Deserialize<JsonElement>(memberGrowthContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (memberGrowthData.TryGetProperty("trend", out var trendElement)) {
                    dashboard.MemberGrowthTrend = JsonSerializer.Deserialize<List<MemberGrowthTrendItem>>(
                        trendElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<MemberGrowthTrendItem>();
                }
            }

            // Fetch Trainer Workload
            var workloadResponse = await _apiHelper.GetRawAsync($"/api/reports/trainer-workload{queryParam}");
            if (workloadResponse.IsSuccessStatusCode) {
                var workloadContent = await workloadResponse.Content.ReadAsStringAsync();
                var workloadData = JsonSerializer.Deserialize<JsonElement>(workloadContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (workloadData.TryGetProperty("workload", out var workloadElement)) {
                    dashboard.TrainerWorkload = JsonSerializer.Deserialize<List<TrainerWorkloadItem>>(
                        workloadElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TrainerWorkloadItem>();
                }
            }

            return View(dashboard);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Dashboard yüklenirken hata oluştu");
            TempData["ErrorMessage"] = "Dashboard yüklenirken bir hata oluştu: " + ex.Message;
            return View(new DashboardViewModel());
        }
    }
}
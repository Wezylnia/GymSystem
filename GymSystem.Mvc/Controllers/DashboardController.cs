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

            // Fetch Dashboard Stats
            var statsEndpoint = gymLocationId.HasValue
                ? ApiEndpoints.ReportsGymOwnerDashboardByLocation(gymLocationId.Value)
                : ApiEndpoints.ReportsGymOwnerDashboard;
            var statsResponse = await _apiHelper.GetRawAsync(statsEndpoint);
            if (statsResponse.IsSuccessStatusCode) {
                var statsContent = await statsResponse.Content.ReadAsStringAsync();
                dashboard.Stats = JsonSerializer.Deserialize<DashboardStatsViewModel>(statsContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new DashboardStatsViewModel();
            }

            // Fetch Membership Statistics
            var membershipEndpoint = gymLocationId.HasValue
                ? ApiEndpoints.ReportsMembershipStatisticsByLocation(gymLocationId.Value)
                : ApiEndpoints.ReportsMembershipStatistics;
            var membershipResponse = await _apiHelper.GetRawAsync(membershipEndpoint);
            if (membershipResponse.IsSuccessStatusCode) {
                var membershipContent = await membershipResponse.Content.ReadAsStringAsync();
                dashboard.MembershipStats = JsonSerializer.Deserialize<MembershipStatisticsViewModel>(membershipContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new MembershipStatisticsViewModel();
            }

            // Fetch Revenue Trend
            var revenueTrendEndpoint = gymLocationId.HasValue
                ? ApiEndpoints.ReportsRevenueTrendByLocation(gymLocationId.Value)
                : ApiEndpoints.ReportsRevenueTrend;
            var revenueTrendResponse = await _apiHelper.GetRawAsync(revenueTrendEndpoint);
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
            var memberGrowthEndpoint = gymLocationId.HasValue
                ? ApiEndpoints.ReportsMemberGrowthTrendByLocation(gymLocationId.Value)
                : ApiEndpoints.ReportsMemberGrowthTrend;
            var memberGrowthResponse = await _apiHelper.GetRawAsync(memberGrowthEndpoint);
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
            var workloadEndpoint = gymLocationId.HasValue
                ? ApiEndpoints.ReportsTrainerWorkloadByLocation(gymLocationId.Value)
                : ApiEndpoints.ReportsTrainerWorkload;
            var workloadResponse = await _apiHelper.GetRawAsync(workloadEndpoint);
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
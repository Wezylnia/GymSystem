namespace GymSystem.Mvc.Helpers;

/// <summary>
/// API Endpoint'leri için enum
/// </summary>
public static class ApiEndpoints {
    // Members
    public const string Members = "/api/members";
    public const string MembersCreate = "/api/members";
    public const string MembersRegister = "/api/members/register"; // Register için AllowAnonymous endpoint
    public static string MemberById(int id) => $"/api/members/{id}";
    public static string MemberByEmail(string email) => $"/api/members/by-email/{email}";

    // GymLocations
    public const string GymLocations = "/api/gymlocations";
    public static string GymLocationById(int id) => $"/api/gymlocations/{id}";

    // Services
    public const string Services = "/api/services";
    public static string ServiceById(int id) => $"/api/services/{id}";

    // Trainers
    public const string Trainers = "/api/trainers";
    public static string TrainerById(int id) => $"/api/trainers/{id}";

    // Appointments
    public const string Appointments = "/api/appointments";
    public static string AppointmentById(int id) => $"/api/appointments/{id}";
    public static string AppointmentsByMember(int memberId) => $"/api/appointments/member/{memberId}";
    public const string AvailableTrainers = "/api/appointments/available-trainers";

    // Membership Requests
    public const string MembershipRequests = "/api/membershiprequests";
    public static string MembershipRequestById(int id) => $"/api/membershiprequests/{id}";
    public static string MembershipRequestsByMember(int memberId) => $"/api/membershiprequests/member/{memberId}";
    public const string MembershipRequestsPending = "/api/membershiprequests/pending";
    public const string MembershipRequestsCreate = "/api/membershiprequests/create";
    public static string MembershipRequestApprove(int id) => $"/api/membershiprequests/{id}/approve";
    public static string MembershipRequestReject(int id) => $"/api/membershiprequests/{id}/reject";

    // AI Workout Plans
    public const string AIWorkoutPlans = "/api/aiworkoutplans";
    public static string AIWorkoutPlanById(int id) => $"/api/aiworkoutplans/{id}";
    public static string AIWorkoutPlansByMember(int memberId) => $"/api/aiworkoutplans/member/{memberId}";

    // Body Measurements
    public const string BodyMeasurements = "/api/bodymeasurements";
    public static string BodyMeasurementById(int id) => $"/api/bodymeasurements/{id}";
    public static string BodyMeasurementsByMember(int memberId) => $"/api/bodymeasurements/member/{memberId}";
    public static string BodyMeasurementsChart(int memberId) => $"/api/bodymeasurements/chart/{memberId}";

    // Reports
    public const string ReportsPopularServices = "/api/reports/popular-services";
    public const string ReportsAvailableTrainers = "/api/reports/available-trainers";
    public const string ReportsMonthlyRevenue = "/api/reports/monthly-revenue";
    public const string ReportsGymOwnerDashboard = "/api/reports/gym-owner-dashboard";
    public const string ReportsMembershipStatistics = "/api/reports/membership-statistics";
    public const string ReportsRevenueTrend = "/api/reports/revenue-trend";
    public const string ReportsMemberGrowthTrend = "/api/reports/member-growth-trend";
    public const string ReportsTrainerWorkload = "/api/reports/trainer-workload";

    public static string ReportsGymOwnerDashboardByLocation(int gymLocationId) => $"/api/reports/gym-owner-dashboard?gymLocationId={gymLocationId}";
    public static string ReportsMembershipStatisticsByLocation(int gymLocationId) => $"/api/reports/membership-statistics?gymLocationId={gymLocationId}";
    public static string ReportsRevenueTrendByLocation(int gymLocationId) => $"/api/reports/revenue-trend?gymLocationId={gymLocationId}";
    public static string ReportsMemberGrowthTrendByLocation(int gymLocationId) => $"/api/reports/member-growth-trend?gymLocationId={gymLocationId}";
    public static string ReportsTrainerWorkloadByLocation(int gymLocationId) => $"/api/reports/trainer-workload?gymLocationId={gymLocationId}";

    // Appointments - Additional
    public static string AppointmentConfirm(int id) => $"/api/appointments/{id}/confirm";
    public static string AppointmentCancel(int id) => $"/api/appointments/{id}/cancel";
}
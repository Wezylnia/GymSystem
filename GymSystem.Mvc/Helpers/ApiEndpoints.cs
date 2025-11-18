namespace GymSystem.Mvc.Helpers;

/// <summary>
/// API Endpoint'leri için enum
/// </summary>
public static class ApiEndpoints
{
    // Members
    public const string Members = "/api/members";
    public static string MemberById(int id) => $"/api/members/{id}";

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

    // Reports
    public const string ReportsPopularServices = "/api/reports/popular-services";
    public const string ReportsAvailableTrainers = "/api/reports/available-trainers";
    public const string ReportsMonthlyRevenue = "/api/reports/monthly-revenue";
    public const string ReportsGymOwnerDashboard = "/api/reports/gym-owner-dashboard";
}

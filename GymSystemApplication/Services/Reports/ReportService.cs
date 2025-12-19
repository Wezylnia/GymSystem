using GymSystem.Application.Abstractions.Services.IAppointmentService;
using GymSystem.Application.Abstractions.Services.IAppointmentService.Contract;
using GymSystem.Application.Abstractions.Services.IGymLocationService;
using GymSystem.Application.Abstractions.Services.IGymLocationService.Contract;
using GymSystem.Application.Abstractions.Services.IMembershipRequestService;
using GymSystem.Application.Abstractions.Services.IReportService;
using GymSystem.Application.Abstractions.Services.IServiceService;
using GymSystem.Application.Abstractions.Services.IServiceService.Contract;
using GymSystem.Application.Abstractions.Services.ITrainerService;
using GymSystem.Application.Abstractions.Services.ITrainerService.Contract;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Reports;

public class ReportService : IReportService {
    private readonly BaseFactory<ReportService> _baseFactory;
    private readonly IAppointmentService _appointmentService;
    private readonly ITrainerService _trainerService;
    private readonly IServiceService _serviceService;
    private readonly IMembershipRequestService _membershipRequestService;
    private readonly IGymLocationService _gymLocationService;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        BaseFactory<ReportService> baseFactory,
        IAppointmentService appointmentService,
        ITrainerService trainerService,
        IServiceService serviceService,
        IMembershipRequestService membershipRequestService,
        IGymLocationService gymLocationService,
        ILogger<ReportService> logger) {
        _baseFactory = baseFactory;
        _appointmentService = appointmentService;
        _trainerService = trainerService;
        _serviceService = serviceService;
        _membershipRequestService = membershipRequestService;
        _gymLocationService = gymLocationService;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = logger;
    }

    public async Task<ServiceResponse<object>> GetTrainersBySpecialtyAsync(string specialty) {
        try {
            var servicesResponse = await _serviceService.GetAllAsync();
            if (!servicesResponse.IsSuccessful || servicesResponse.Data == null)
                return _responseHelper.SetError<object>(null, "Hizmetler alınamadı", 500, "REPORT_001");

            var matchingService = servicesResponse.Data?.Where(s => s.Name.Contains(specialty, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (matchingService == null)
                return _responseHelper.SetSuccess<object>(new { trainers = Array.Empty<object>(), message = $"'{specialty}' uzmanlığında hizmet bulunamadı" });

            var trainersResponse = await _trainerService.GetAllAsync();
            if (!trainersResponse.IsSuccessful)
                return _responseHelper.SetError<object>(null, "Antrenörler alınamadı", 500, "REPORT_002");

            var trainers = trainersResponse.Data?.Where(t => t.GymLocationId == matchingService.GymLocationId && t.IsActive).Select(t => new { t.Id, FullName = $"{t.FirstName} {t.LastName}", t.Email, t.PhoneNumber, t.Bio, Specialty = specialty }).ToList();

            if (trainers == null || !trainers.Any())
                return _responseHelper.SetSuccess<object>(new { trainers = Array.Empty<object>(), count = 0 });

            return _responseHelper.SetSuccess<object>(new { trainers, count = trainers.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenörler specialty'ye göre getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_003", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetAvailableTrainersWithDetailsAsync(int serviceId, DateTime appointmentDateTime, int durationMinutes) {
        try {
            var availableTrainersResponse = await _appointmentService.GetAvailableTrainersAsync(serviceId, appointmentDateTime, durationMinutes);

            if (!availableTrainersResponse.IsSuccessful)
                return _responseHelper.SetError<object>(null, availableTrainersResponse.Error?.ErrorMessage ?? "Uygun antrenörler alınamadı", availableTrainersResponse.Error?.StatusCode ?? 500, availableTrainersResponse.Error?.ErrorCode ?? "REPORT_004");

            var trainersResponse = await _trainerService.GetAllAsync();
            if (!trainersResponse.IsSuccessful || trainersResponse.Data == null)
                return _responseHelper.SetSuccess<object>(new { trainers = Array.Empty<object>(), count = 0 });

            var availableIds = availableTrainersResponse.Data?.ToHashSet() ?? new HashSet<int>();
            var trainerDetails = trainersResponse.Data.Where(t => availableIds.Contains(t.Id)).Select(t => new { t.Id, FullName = $"{t.FirstName} {t.LastName}", t.Email, t.PhoneNumber, AvailableDate = appointmentDateTime.ToString("dd.MM.yyyy"), AvailableTime = appointmentDateTime.ToString("HH:mm") }).ToList();

            return _responseHelper.SetSuccess<object>(new { trainers = trainerDetails, count = trainerDetails.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Uygun antrenörler getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_005", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetMemberAppointmentsWithDetailsAsync(int memberId) {
        try {
            var response = await _appointmentService.GetMemberAppointmentsAsync(memberId);

            if (!response.IsSuccessful)
                return _responseHelper.SetError<object>(null, response.Error?.ErrorMessage ?? "Randevular alınamadı", response.Error?.StatusCode ?? 500, response.Error?.ErrorCode ?? "REPORT_006");

            var appointments = response.Data?.OrderByDescending(a => a.AppointmentDate).Select(a => new { a.Id, Date = a.AppointmentDate.ToString("dd.MM.yyyy HH:mm"), a.DurationMinutes, a.Price, a.Status, a.Notes }).ToList();

            if (appointments == null || !appointments.Any())
                return _responseHelper.SetSuccess<object>(new { appointments = Array.Empty<object>(), count = 0, memberId });

            return _responseHelper.SetSuccess<object>(new { appointments, count = appointments.Count, memberId });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye randevuları getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_007", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetPopularServicesAsync(int top = 5) {
        try {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var servicesResponse = await _serviceService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !servicesResponse.IsSuccessful)
                return _responseHelper.SetError<object>(null, "Veriler alınamadı", 500, "REPORT_008");

            var appointments = appointmentsResponse.Data?.Where(a => a.IsActive && a.Status == AppointmentStatus.Confirmed.ToString()).ToList() ?? new List<AppointmentDto>();
            var services = servicesResponse.Data ?? new List<ServiceDto>();

            var popularServices = appointments.GroupBy(a => a.ServiceId).Select(g => new { ServiceId = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(top).Join(services, ps => ps.ServiceId, s => s.Id, (ps, s) => new { s.Id, s.Name, s.Description, s.Price, s.DurationMinutes, AppointmentCount = ps.Count, s.GymLocationName }).ToList();

            return _responseHelper.SetSuccess<object>(new { Services = popularServices, Count = popularServices.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Popüler hizmetler getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_008", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetMonthlyRevenueAsync(int month, int year) {
        try {
            var appointmentRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();

            var monthlyData = await appointmentRepository.QueryNoTracking().Where(a => a.AppointmentDate.Year == year && a.AppointmentDate.Month == month && a.Status == AppointmentStatus.Confirmed && a.IsActive).GroupBy(a => 1).Select(g => new { Month = $"{month}/{year}", TotalAppointments = g.Count(), TotalRevenue = g.Sum(a => a.Price), AveragePrice = g.Average(a => a.Price), MinPrice = g.Min(a => a.Price), MaxPrice = g.Max(a => a.Price) }).FirstOrDefaultAsync();

            if (monthlyData == null)
                return _responseHelper.SetSuccess<object>(new { Month = $"{month}/{year}", TotalAppointments = 0, TotalRevenue = 0m, AveragePrice = 0m, MinPrice = 0m, MaxPrice = 0m });

            return _responseHelper.SetSuccess<object>(monthlyData);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Aylık gelir raporu getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_011", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetTrainerWorkloadAsync(int? trainerId = null) {
        try {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var trainersResponse = await _trainerService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !trainersResponse.IsSuccessful)
                return _responseHelper.SetError<object>(null, "Veriler alınamadı", 500, "REPORT_011");

            var appointments = appointmentsResponse.Data?.Where(a => a.IsActive).ToList() ?? new List<AppointmentDto>();
            var trainers = trainersResponse.Data ?? new List<TrainerDto>();

            if (trainerId.HasValue)
                appointments = appointments.Where(a => a.TrainerId == trainerId.Value).ToList();

            var workload = appointments.GroupBy(a => a.TrainerId).Select(g => {
                var trainer = trainers.FirstOrDefault(t => t.Id == g.Key);
                var trainerAppointments = g.ToList();
                return new {
                    TrainerId = g.Key,
                    TrainerName = trainer != null ? $"{trainer.FirstName} {trainer.LastName}" : "Unknown",
                    TotalAppointments = trainerAppointments.Count,
                    PendingAppointments = trainerAppointments.Count(a => a.Status == AppointmentStatus.Pending.ToString()),
                    ConfirmedAppointments = trainerAppointments.Count(a => a.Status == AppointmentStatus.Confirmed.ToString()),
                    CompletedAppointments = trainerAppointments.Count(a => a.Status == AppointmentStatus.Completed.ToString()),
                    TotalHours = trainerAppointments.Sum(a => a.DurationMinutes) / 60.0,
                    TotalRevenue = trainerAppointments.Where(a => a.Status == AppointmentStatus.Confirmed.ToString() || a.Status == AppointmentStatus.Completed.ToString()).Sum(a => a.Price)
                };
            }).OrderByDescending(x => x.TotalAppointments).ToList();

            return _responseHelper.SetSuccess<object>(new { Workload = workload, TotalTrainers = workload.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör iş yükü getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_011", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetGymOwnerDashboardStatsAsync(int? gymLocationId = null) {
        try {
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var appointmentRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var trainerRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Trainer>();
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();

            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);

            // EF Core DbContext thread-safe olmadığı için sorguları sıralı çalıştır
            // Üyeler: CurrentGymLocationId olan (yani bir salona kayıtlı) üyeleri çek
            var members = await memberRepository.QueryNoTracking()
                .Where(m => m.IsActive && m.CurrentGymLocationId.HasValue && (!gymLocationId.HasValue || m.CurrentGymLocationId == gymLocationId.Value))
                .Select(m => new { m.Id, m.MembershipEndDate, m.CurrentGymLocationId })
                .ToListAsync();

            var trainers = await trainerRepository.QueryNoTracking()
                .Where(t => t.IsActive && (!gymLocationId.HasValue || t.GymLocationId == gymLocationId.Value))
                .Select(t => new { t.Id, t.GymLocationId })
                .ToListAsync();

            var requests = await requestRepository.QueryNoTracking()
                .Where(mr => mr.IsActive && (!gymLocationId.HasValue || mr.GymLocationId == gymLocationId.Value))
                .ToListAsync();

            // Randevuları Trainer ile birlikte çek, gymLocationId filtrelemesi için
            var appointments = await appointmentRepository.QueryNoTracking()
                .Include(a => a.Trainer)
                .Where(a => a.IsActive)
                .ToListAsync();

            var trainersCount = trainers.Count;

            // gymLocationId varsa randevuları filtrele
            if (gymLocationId.HasValue) {
                var gymTrainerIds = trainers.Where(t => t.GymLocationId == gymLocationId.Value).Select(t => t.Id).ToHashSet();
                appointments = appointments.Where(a => gymTrainerIds.Contains(a.TrainerId)).ToList();
            }

            var totalMembers = members.Count;
            // Aktif üye: MembershipEndDate > now olan ve CurrentGymLocationId'si olan üyeler
            var activeMembers = members.Count(m => m.MembershipEndDate.HasValue && m.MembershipEndDate.Value > now);
            var pendingRequests = requests.Count(r => r.Status == MembershipRequestStatus.Pending);
            var thisMonthRequests = requests.Count(r => r.CreatedAt.Year == now.Year && r.CreatedAt.Month == now.Month);
            var thisMonthRevenue = appointments.Where(a => a.AppointmentDate.Year == now.Year && a.AppointmentDate.Month == now.Month && a.Status == AppointmentStatus.Confirmed).Sum(a => a.Price);
            var lastMonthRevenue = appointments.Where(a => a.AppointmentDate.Year == lastMonth.Year && a.AppointmentDate.Month == lastMonth.Month && a.Status == AppointmentStatus.Confirmed).Sum(a => a.Price);
            var revenueGrowth = lastMonthRevenue > 0 ? ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 : 0;

            return _responseHelper.SetSuccess<object>(new { TotalMembers = totalMembers, ActiveMembers = activeMembers, TotalTrainers = trainersCount, TotalAppointments = appointments.Count, PendingMembershipRequests = pendingRequests, ThisMonthRevenue = thisMonthRevenue, LastMonthRevenue = lastMonthRevenue, RevenueGrowth = Math.Round(revenueGrowth, 2), ThisMonthRequests = thisMonthRequests });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Dashboard istatistikleri getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_014", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetMembershipStatisticsAsync(int? gymLocationId = null) {
        try {
            var requestsResponse = await _membershipRequestService.GetAllRequestsAsync();

            if (!requestsResponse.IsSuccessful || requestsResponse.Data == null)
                return _responseHelper.SetError<object>(null, "Talepler alınamadı", 500, "REPORT_015");

            var requests = requestsResponse.Data;

            if (gymLocationId.HasValue)
                requests = requests.Where(mr => mr.GymLocationId == gymLocationId.Value).ToList();

            var stats = new { Total = requests.Count, Pending = requests.Count(r => r.Status == "Pending"), Approved = requests.Count(r => r.Status == "Approved"), Rejected = requests.Count(r => r.Status == "Rejected"), ByDuration = requests.GroupBy(r => r.Duration).Select(g => new { Duration = g.Key.ToString(), Count = g.Count(), TotalRevenue = g.Where(r => r.Status == "Approved").Sum(r => r.Price) }).ToList() };

            return _responseHelper.SetSuccess<object>(stats);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üyelik istatistikleri getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_015", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetRevenueByGymLocationAsync() {
        try {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var trainersResponse = await _trainerService.GetAllAsync();
            var gymLocationsResponse = await _gymLocationService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !trainersResponse.IsSuccessful || !gymLocationsResponse.IsSuccessful)
                return _responseHelper.SetError<object>(null, "Veriler alınamadı", 500, "REPORT_016");

            var appointments = appointmentsResponse.Data?.Where(a => a.IsActive && a.Status == AppointmentStatus.Confirmed.ToString()).ToList() ?? new List<AppointmentDto>();
            var trainers = trainersResponse.Data ?? new List<TrainerDto>();
            var gymLocations = gymLocationsResponse.Data ?? new List<GymLocationDto>();

            var revenueByGym = appointments.Join(trainers, a => a.TrainerId, t => t.Id, (a, t) => new { Appointment = a, Trainer = t }).GroupBy(x => x.Trainer.GymLocationId).Select(g => new { GymLocationId = g.Key, TotalRevenue = g.Sum(x => x.Appointment.Price), AppointmentCount = g.Count() }).Join(gymLocations, r => r.GymLocationId, g => g.Id, (r, g) => new { GymLocationId = g.Id, GymLocationName = g.Name, r.TotalRevenue, r.AppointmentCount }).OrderByDescending(x => x.TotalRevenue).ToList();

            return _responseHelper.SetSuccess<object>(new { RevenueByGym = revenueByGym, TotalGyms = revenueByGym.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Salonlara göre gelir dağılımı getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_016", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetRevenueTrendAsync(int? gymLocationId = null) {
        try {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var trainersResponse = await _trainerService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !trainersResponse.IsSuccessful)
                return _responseHelper.SetError<object>(null, "Veriler alınamadı", 500, "REPORT_017");

            var appointments = appointmentsResponse.Data?.Where(a => a.IsActive && a.Status == AppointmentStatus.Confirmed.ToString()).ToList() ?? new List<AppointmentDto>();
            var trainers = trainersResponse.Data ?? new List<TrainerDto>();

            if (gymLocationId.HasValue) {
                var gymTrainerIds = trainers.Where(t => t.GymLocationId == gymLocationId.Value).Select(t => t.Id).ToHashSet();
                appointments = appointments.Where(a => gymTrainerIds.Contains(a.TrainerId)).ToList();
            }

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var trend = appointments.Where(a => a.AppointmentDate >= sixMonthsAgo).GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month }).Select(g => new { Month = $"{g.Key.Month:00}/{g.Key.Year}", Year = g.Key.Year, MonthNumber = g.Key.Month, Revenue = g.Sum(a => a.Price), AppointmentCount = g.Count() }).OrderBy(x => x.Year).ThenBy(x => x.MonthNumber).ToList();

            return _responseHelper.SetSuccess<object>(new { Trend = trend, Months = trend.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Gelir trendi getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_017", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<object>> GetMemberGrowthTrendAsync(int? gymLocationId = null) {
        try {
            var requestsResponse = await _membershipRequestService.GetAllRequestsAsync();

            if (!requestsResponse.IsSuccessful || requestsResponse.Data == null)
                return _responseHelper.SetError<object>(null, "Talepler alınamadı", 500, "REPORT_018");

            var requests = requestsResponse.Data;

            if (gymLocationId.HasValue)
                requests = requests.Where(mr => mr.GymLocationId == gymLocationId.Value).ToList();

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var trend = requests.Where(r => r.Status == "Approved" && r.ApprovedAt.HasValue && r.ApprovedAt >= sixMonthsAgo).GroupBy(r => new { r.ApprovedAt!.Value.Year, r.ApprovedAt.Value.Month }).Select(g => new { Month = $"{g.Key.Month:00}/{g.Key.Year}", Year = g.Key.Year, MonthNumber = g.Key.Month, NewMembers = g.Count(), TotalRevenue = g.Sum(r => r.Price) }).OrderBy(x => x.Year).ThenBy(x => x.MonthNumber).ToList();

            return _responseHelper.SetSuccess<object>(new { Trend = trend, Months = trend.Count });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye artış trendi getirilirken hata oluştu");
            return _responseHelper.SetError<object>(null, new ErrorInfo(ex.Message, "REPORT_018", ex.StackTrace, 500));
        }
    }
}
namespace GymSystem.Mvc.Models;

public class DashboardViewModel {
    public DashboardStatsViewModel Stats { get; set; } = new();
    public MembershipStatisticsViewModel MembershipStats { get; set; } = new();
    public List<RevenueTrendItem> RevenueTrend { get; set; } = new();
    public List<MemberGrowthTrendItem> MemberGrowthTrend { get; set; } = new();
    public List<TrainerWorkloadItem> TrainerWorkload { get; set; } = new();
}

public class DashboardStatsViewModel {
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalAppointments { get; set; }
    public int PendingMembershipRequests { get; set; }
    public decimal ThisMonthRevenue { get; set; }
    public decimal LastMonthRevenue { get; set; }
    public double RevenueGrowth { get; set; }
    public int ThisMonthRequests { get; set; }
}

public class MembershipStatisticsViewModel {
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public List<MembershipByDuration> ByDuration { get; set; } = new();
}

public class MembershipByDuration {
    public string Duration { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class RevenueTrendItem {
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int MonthNumber { get; set; }
    public decimal Revenue { get; set; }
    public int AppointmentCount { get; set; }
}

public class MemberGrowthTrendItem {
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int MonthNumber { get; set; }
    public int NewMembers { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TrainerWorkloadItem {
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public double TotalHours { get; set; }
    public decimal TotalRevenue { get; set; }
}

namespace GymSystem.Common.Helpers;

/// <summary>
/// PostgreSQL timestamp without time zone için DateTime helper
/// </summary>
public static class DateTimeHelper {
    /// <summary>
    /// PostgreSQL-safe DateTime.Now (Unspecified Kind)
    /// </summary>
    public static DateTime Now => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

    /// <summary>
    /// PostgreSQL-safe DateTime.UtcNow (Unspecified Kind)
    /// </summary>
    public static DateTime UtcNow => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

    /// <summary>
    /// DateTime'ı PostgreSQL-safe hale getirir
    /// </summary>
    public static DateTime ToUnspecified(this DateTime dateTime) {
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Nullable DateTime'ı PostgreSQL-safe hale getirir
    /// </summary>
    public static DateTime? ToUnspecified(this DateTime? dateTime) {
        return dateTime.HasValue ? DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Unspecified) : null;
    }

    /// <summary>
    /// AddMonths ile PostgreSQL-safe DateTime döndürür
    /// </summary>
    public static DateTime AddMonthsSafe(this DateTime dateTime, int months) {
        return DateTime.SpecifyKind(dateTime.AddMonths(months), DateTimeKind.Unspecified);
    }

    /// <summary>
    /// AddDays ile PostgreSQL-safe DateTime döndürür
    /// </summary>
    public static DateTime AddDaysSafe(this DateTime dateTime, int days) {
        return DateTime.SpecifyKind(dateTime.AddDays(days), DateTimeKind.Unspecified);
    }
}
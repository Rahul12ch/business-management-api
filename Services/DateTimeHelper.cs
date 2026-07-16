namespace client.Helpers;

public static class DateTimeHelper
{
    private static readonly TimeZoneInfo IndiaTimeZone =
        OperatingSystem.IsWindows()
            ? TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
            : TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    public static DateTime ToIndia(DateTime dateTime)
{
    if (dateTime.Kind == DateTimeKind.Local)
        dateTime = dateTime.ToUniversalTime();

    if (dateTime.Kind == DateTimeKind.Unspecified)
        dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

    return TimeZoneInfo.ConvertTimeFromUtc(dateTime, IndiaTimeZone);
}
    public static DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}
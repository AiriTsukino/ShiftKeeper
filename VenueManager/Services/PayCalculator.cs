using VenueManager.Models;

namespace VenueManager.Services;

public static class PayCalculator
{
    public static long CalculateGrossPay(StaffMember member, NightlyStaffRecord record, VenueProfile venue, Configuration config)
    {
        var rate = Math.Max(0d, member.PayRate);
        var assignedShiftCount = Math.Max(1, venue.GetShifts(member).Count);
        var assignedShiftSeconds = Math.Max(1d, venue.GetAssignedShiftDurationSeconds(member));
        var raw = member.PayType switch
        {
            PayType.Hourly => rate * (record.AccruedSeconds / 3600d),
            PayType.PerShift when config.CountUpPerShiftPay && member.PresenceMode != PresenceMode.NoTimer =>
                rate * assignedShiftCount * Math.Clamp(record.AccruedSeconds / assignedShiftSeconds, 0d, 1d),
            PayType.PerShift => record.HasWorked ? rate * assignedShiftCount : 0f,
            _ => 0f,
        };

        return RoundToGil(raw, config.PayRoundingIncrement);
    }

    public static long CalculateRemainingDue(StaffMember member, NightlyStaffRecord record, VenueProfile venue, Configuration config) =>
        Math.Max(0, CalculateGrossPay(member, record, venue, config) - record.TotalPaidGil);

    public static long CalculateMaximumShiftPay(StaffMember member, VenueProfile venue, Configuration config)
    {
        var rate = Math.Max(0d, member.PayRate);
        var shiftSeconds = Math.Max(1d, venue.GetAssignedShiftDurationSeconds(member));
        var assignedShiftCount = Math.Max(1, venue.GetShifts(member).Count);
        var raw = member.PayType == PayType.Hourly ? rate * (shiftSeconds / 3600d) : rate * assignedShiftCount;
        return RoundToGil(raw, config.PayRoundingIncrement);
    }

    public static bool IsWorkComplete(StaffMember member, NightlyStaffRecord record, VenueProfile venue)
    {
        if (record.ShiftEndedEarly) return true;
        if (!record.HasWorked) return false;
        if (member.PresenceMode == PresenceMode.NoTimer) return true;
        var shiftSeconds = venue.GetAssignedShiftDurationSeconds(member);
        return shiftSeconds > 0 && record.AccruedSeconds + 1d >= shiftSeconds;
    }

    private static long RoundToGil(double raw, int configuredIncrement)
    {
        var increment = configuredIncrement > 0 ? configuredIncrement : 1;
        var roundedUnits = Math.Round(Math.Max(0d, raw) / increment, MidpointRounding.AwayFromZero);
        return checked((long)Math.Min(long.MaxValue / (double)increment, roundedUnits) * increment);
    }
}

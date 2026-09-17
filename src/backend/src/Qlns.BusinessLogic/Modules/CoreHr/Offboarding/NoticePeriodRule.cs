namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Notice-period comparison of SRS EMP-07 step 2: a shortfall is reported as a warning
/// (<c>noticePeriodShortfallDays</c>) and audited, never enforced — the decision belongs to the HR Manager.
/// </summary>
public static class NoticePeriodRule
{
    /// <summary>
    /// Days by which the notice given falls short of the contractual notice period; 0 when sufficient;
    /// null when no notice date was recorded or the contract does not define a notice period.
    /// </summary>
    public static int? ShortfallDays(DateOnly? noticeReceivedOn, DateOnly lastWorkingDate, int? noticePeriodDays)
    {
        if (noticeReceivedOn is not { } received || noticePeriodDays is not { } required)
        {
            return null;
        }

        var noticeGiven = lastWorkingDate.DayNumber - received.DayNumber;
        return Math.Max(0, required - noticeGiven);
    }
}

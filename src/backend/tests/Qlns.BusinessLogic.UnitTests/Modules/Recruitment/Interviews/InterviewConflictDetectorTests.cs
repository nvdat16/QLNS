using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Interviews;

public sealed class InterviewConflictDetectorTests
{
    private static readonly DateTimeOffset T14 = new(2026, 9, 16, 14, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 60, 30, 90, true)]     // partial overlap
    [InlineData(0, 60, 60, 120, false)]   // back-to-back
    [InlineData(0, 60, -60, 0, false)]    // back-to-back before
    [InlineData(0, 60, 10, 20, true)]     // contained
    [InlineData(10, 20, 0, 60, true)]     // containing
    [InlineData(0, 60, 120, 180, false)]  // disjoint
    public void Slot_Overlaps_UsesHalfOpenIntervals(int aStart, int aEnd, int bStart, int bEnd, bool expected)
    {
        var a = new InterviewSlot(T14.AddMinutes(aStart), T14.AddMinutes(aEnd));
        var b = new InterviewSlot(T14.AddMinutes(bStart), T14.AddMinutes(bEnd));

        Assert.Equal(expected, a.Overlaps(b));
        Assert.Equal(expected, b.Overlaps(a));
    }

    [Fact]
    public void FindFirst_NoOverlappingInterview_ReturnsNull()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(0, 60),
            [7],
            "Room A",
            [Scheduled(1, 60, 120, "Room A", 7)]);

        Assert.Null(conflict);
    }

    [Fact]
    public void FindFirst_SharedPanelist_ReturnsInterviewerConflict()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(30, 90),
            [9, 7],
            null,
            [Scheduled(11, 0, 60, "Room B", 7, 8)]);

        Assert.NotNull(conflict);
        Assert.Equal(InterviewConflictKind.Interviewer, conflict.Kind);
        Assert.Equal(7, conflict.InterviewerUserId);
        Assert.Equal(11, conflict.ConflictingInterviewId);
        Assert.Equal(Slot(0, 60), conflict.Slot);
    }

    [Fact]
    public void FindFirst_SameLocationIgnoringCaseAndWhitespace_ReturnsLocationConflict()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(30, 90),
            [9],
            " room a ",
            [Scheduled(12, 0, 60, "Room A", 7)]);

        Assert.NotNull(conflict);
        Assert.Equal(InterviewConflictKind.Location, conflict.Kind);
        Assert.Null(conflict.InterviewerUserId);
        Assert.Equal(12, conflict.ConflictingInterviewId);
    }

    [Fact]
    public void FindFirst_OnlineInterviewWithoutLocation_NeverReportsLocationConflict()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(30, 90),
            [9],
            null,
            [Scheduled(12, 0, 60, null, 7)]);

        Assert.Null(conflict);
    }

    [Fact]
    public void FindFirst_InterviewerConflictTakesPrecedenceOverLocation()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(30, 90),
            [7],
            "Room A",
            [
                Scheduled(1, 0, 60, "Room A", 8),   // earlier, location clash only
                Scheduled(2, 45, 120, "Room Z", 7)  // later, interviewer clash
            ]);

        Assert.NotNull(conflict);
        Assert.Equal(InterviewConflictKind.Interviewer, conflict.Kind);
        Assert.Equal(2, conflict.ConflictingInterviewId);
    }

    [Fact]
    public void FindFirst_ReportsEarliestConflictingInterview()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(0, 120),
            [7],
            null,
            [
                Scheduled(5, 60, 90, null, 7),
                Scheduled(3, 10, 30, null, 7),
                Scheduled(4, 10, 30, null, 7)
            ]);

        Assert.NotNull(conflict);
        Assert.Equal(3, conflict.ConflictingInterviewId);
    }

    [Fact]
    public void FindFirst_ExcludesTheInterviewBeingRescheduled()
    {
        var conflict = InterviewConflictDetector.FindFirst(
            Slot(0, 60),
            [7],
            "Room A",
            [Scheduled(42, 0, 60, "Room A", 7)],
            excludeInterviewId: 42);

        Assert.Null(conflict);
    }

    [Fact]
    public void ToBusinessRule_InterviewerConflict_CarriesCodeAndDetails()
    {
        var conflict = new InterviewConflict(InterviewConflictKind.Interviewer, 7, 11, Slot(0, 60));

        var exception = conflict.ToBusinessRule();

        Assert.Equal(InterviewConflict.InterviewerConflictCode, exception.Code);
        Assert.Equal(7L, exception.Details["interviewerUserId"]);
        Assert.Equal(11L, exception.Details["conflictingInterviewId"]);
        Assert.Equal(T14, exception.Details["startsAt"]);
        Assert.Equal(T14.AddMinutes(60), exception.Details["endsAt"]);
    }

    [Fact]
    public void ToBusinessRule_LocationConflict_CarriesCodeAndDetails()
    {
        var conflict = new InterviewConflict(InterviewConflictKind.Location, null, 12, Slot(0, 60));

        var exception = conflict.ToBusinessRule();

        Assert.Equal(InterviewConflict.LocationConflictCode, exception.Code);
        Assert.Equal(12L, exception.Details["conflictingInterviewId"]);
        Assert.False(exception.Details.ContainsKey("interviewerUserId"));
    }

    private static InterviewSlot Slot(int startMinutes, int endMinutes) =>
        new(T14.AddMinutes(startMinutes), T14.AddMinutes(endMinutes));

    private static ScheduledInterviewSummary Scheduled(long id, int startMinutes, int endMinutes, string? location, params long[] panel) =>
        new(id, Slot(startMinutes, endMinutes), location, panel);
}

using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Interviews;

public sealed class InterviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tomorrow = Now.AddDays(1);

    [Fact]
    public void Schedule_Valid_BuildsUnsavedScheduledInterviewWithLeadFirst()
    {
        var interview = Interview.Schedule(ValidWrite(location: "  Room A  ", meetingUrl: null), Now);

        Assert.Equal(0, interview.Id);
        Assert.Equal(1, interview.Version);
        Assert.Equal(InterviewStatus.Scheduled, interview.Status);
        Assert.Equal(7, interview.LeadInterviewerUserId);
        Assert.Equal([7L, 8L], interview.PanelUserIds);
        Assert.Equal("Room A", interview.Location);
        Assert.Null(interview.MeetingUrl);
        Assert.Equal("Asia/Ho_Chi_Minh", interview.Timezone);
        Assert.Equal("tech", interview.InterviewType);
        Assert.Equal(Now, interview.CreatedAt);
        Assert.Equal(Now, interview.UpdatedAt);
    }

    [Fact]
    public void Schedule_EndsBeforeStart_ThrowsOnEndsAt()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite(startsAt: Tomorrow, endsAt: Tomorrow.AddMinutes(-1)), Now));

        Assert.True(exception.Errors.ContainsKey("endsAt"));
    }

    [Fact]
    public void Schedule_StartInPast_ThrowsOnStartsAt()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite(startsAt: Now.AddHours(-2), endsAt: Now.AddHours(-1)), Now));

        Assert.True(exception.Errors.ContainsKey("startsAt"));
    }

    [Fact]
    public void Schedule_MissingTimes_ThrowsOnBothFields()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { StartsAt = null, EndsAt = null }, Now));

        Assert.True(exception.Errors.ContainsKey("startsAt"));
        Assert.True(exception.Errors.ContainsKey("endsAt"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    public void Schedule_InvalidTimezone_ThrowsOnTimezone(string? timezone)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { Timezone = timezone }, Now));

        Assert.True(exception.Errors.ContainsKey("timezone"));
    }

    [Fact]
    public void Schedule_NoVenue_ThrowsOnLocationAndMeetingUrl()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite(location: "   ", meetingUrl: null), Now));

        Assert.True(exception.Errors.ContainsKey("location"));
        Assert.True(exception.Errors.ContainsKey("meetingUrl"));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://meet.example/room")]
    [InlineData("/relative/path")]
    public void Schedule_InvalidMeetingUrl_ThrowsOnMeetingUrl(string meetingUrl)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite(location: null, meetingUrl: meetingUrl), Now));

        Assert.True(exception.Errors.ContainsKey("meetingUrl"));
    }

    [Fact]
    public void Schedule_MeetingUrlOnly_IsAccepted()
    {
        var interview = Interview.Schedule(ValidWrite(location: null, meetingUrl: " https://meet.example/abc "), Now);

        Assert.Null(interview.Location);
        Assert.Equal("https://meet.example/abc", interview.MeetingUrl);
    }

    [Fact]
    public void Schedule_EmptyPanel_ThrowsOnInterviewerUserIds()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { InterviewerUserIds = [] }, Now));

        Assert.True(exception.Errors.ContainsKey("interviewerUserIds"));
    }

    [Fact]
    public void Schedule_DuplicatePanelist_ThrowsOnInterviewerUserIds()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { InterviewerUserIds = [7, 7] }, Now));

        Assert.True(exception.Errors.ContainsKey("interviewerUserIds"));
    }

    [Fact]
    public void Schedule_NonPositivePanelist_ThrowsOnInterviewerUserIds()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { InterviewerUserIds = [7, 0] }, Now));

        Assert.True(exception.Errors.ContainsKey("interviewerUserIds"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Schedule_BlankInterviewType_ThrowsOnInterviewType(string interviewType)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { InterviewType = interviewType }, Now));

        Assert.True(exception.Errors.ContainsKey("interviewType"));
    }

    [Fact]
    public void Schedule_TooLongInterviewType_ThrowsOnInterviewType()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Interview.Schedule(ValidWrite() with { InterviewType = new string('x', Interview.InterviewTypeMaxLength + 1) }, Now));

        Assert.True(exception.Errors.ContainsKey("interviewType"));
    }

    [Fact]
    public void Reschedule_Scheduled_MovesSlotKeepsVenueAndBumpsVersion()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 3);
        var newStart = Tomorrow.AddDays(1);

        interview.Reschedule(new InterviewChange(newStart, newStart.AddHours(2), null, null, null, null), Now);

        Assert.Equal(new InterviewSlot(newStart, newStart.AddHours(2)), interview.Slot);
        Assert.Equal("Room A", interview.Location);
        Assert.Equal("Asia/Ho_Chi_Minh", interview.Timezone);
        Assert.Equal(4, interview.Version);
        Assert.Equal(Now, interview.UpdatedAt);
        Assert.Equal(InterviewStatus.Scheduled, interview.Status);
    }

    [Fact]
    public void Reschedule_WithNewVenueAndTimezone_ReplacesThem()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1);

        interview.Reschedule(
            new InterviewChange(Tomorrow, Tomorrow.AddHours(1), "Europe/Paris", "", "https://meet.example/x", null),
            Now);

        Assert.Null(interview.Location);
        Assert.Equal("https://meet.example/x", interview.MeetingUrl);
        Assert.Equal("Europe/Paris", interview.Timezone);
    }

    [Fact]
    public void Reschedule_WithoutTimes_ThrowsValidation()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => interview.Reschedule(null, Now));

        Assert.True(exception.Errors.ContainsKey("startsAt"));
        Assert.True(exception.Errors.ContainsKey("endsAt"));
        Assert.Equal(1, interview.Version);
    }

    [Fact]
    public void Reschedule_ClearingOnlyVenue_ThrowsValidation()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            interview.Reschedule(new InterviewChange(Tomorrow, Tomorrow.AddHours(1), null, "", null, null), Now));

        Assert.True(exception.Errors.ContainsKey("location"));
        Assert.Equal("Room A", interview.Location);
    }

    [Theory]
    [InlineData(InterviewStatus.Completed)]
    [InlineData(InterviewStatus.Cancelled)]
    [InlineData(InterviewStatus.NoShow)]
    public void Reschedule_NotScheduled_ThrowsInvalidTransition(InterviewStatus status)
    {
        var interview = CreateInterview(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            interview.Reschedule(new InterviewChange(Tomorrow, Tomorrow.AddHours(1), null, null, null, null), Now));

        Assert.Equal(Interview.InvalidTransitionCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal("reschedule", exception.Details["action"]);
    }

    [Fact]
    public void Complete_AfterStart_CompletesAndBumpsVersion()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 2, startsAt: Now.AddHours(-1));

        interview.Complete(Now);

        Assert.Equal(InterviewStatus.Completed, interview.Status);
        Assert.Equal(3, interview.Version);
        Assert.Equal(Now, interview.UpdatedAt);
    }

    [Fact]
    public void Complete_ExactlyAtStart_IsAllowed()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1, startsAt: Now);

        interview.Complete(Now);

        Assert.Equal(InterviewStatus.Completed, interview.Status);
    }

    [Fact]
    public void Complete_BeforeStart_ThrowsNotStarted()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1, startsAt: Tomorrow);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => interview.Complete(Now));

        Assert.Equal(Interview.NotStartedCode, exception.Code);
        Assert.Equal(InterviewStatus.Scheduled, interview.Status);
        Assert.Equal(1, interview.Version);
    }

    [Theory]
    [InlineData(InterviewStatus.Completed)]
    [InlineData(InterviewStatus.Cancelled)]
    public void Complete_NotScheduled_ThrowsInvalidTransition(InterviewStatus status)
    {
        var interview = CreateInterview(status, version: 1, startsAt: Now.AddHours(-1));

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => interview.Complete(Now));

        Assert.Equal(Interview.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Cancel_WithReason_CancelsAndStoresTrimmedReason()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 5);

        interview.Cancel("  Candidate withdrew  ", Now);

        Assert.Equal(InterviewStatus.Cancelled, interview.Status);
        Assert.Equal("Candidate withdrew", interview.CancellationReason);
        Assert.Equal(6, interview.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => interview.Cancel(reason, Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(InterviewStatus.Scheduled, interview.Status);
    }

    [Fact]
    public void Cancel_TooLongReason_ThrowsValidationOnReason()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            interview.Cancel(new string('r', Interview.ReasonMaxLength + 1), Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
    }

    [Fact]
    public void Cancel_Completed_ThrowsInvalidTransition()
    {
        var interview = CreateInterview(InterviewStatus.Completed, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => interview.Cancel("reason", Now));

        Assert.Equal(Interview.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void IsPanelist_ReflectsPanelMembership()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 1);

        Assert.True(interview.IsPanelist(7));
        Assert.True(interview.IsPanelist(8));
        Assert.False(interview.IsPanelist(9));
    }

    [Fact]
    public void Snapshot_CapturesScheduleFieldsAndVersion()
    {
        var interview = CreateInterview(InterviewStatus.Scheduled, version: 4);

        var snapshot = interview.Snapshot();

        Assert.Equal(interview.Slot, snapshot.Slot);
        Assert.Equal("Room A", snapshot.Location);
        Assert.Equal(InterviewStatus.Scheduled, snapshot.Status);
        Assert.Equal(4, snapshot.Version);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        var slot = new InterviewSlot(Tomorrow, Tomorrow.AddHours(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Interview(
            0, 1, "tech", slot, "UTC", [7], "Room", null, InterviewStatus.Completed, null, 1, Now, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Interview(
            1, 0, "tech", slot, "UTC", [7], "Room", null, InterviewStatus.Scheduled, null, 1, Now, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Interview(
            1, 1, "tech", slot, "UTC", [7], "Room", null, InterviewStatus.Scheduled, null, 0, Now, Now));
        Assert.Throws<ArgumentException>(() => new Interview(
            1, 1, "tech", slot, "UTC", [], "Room", null, InterviewStatus.Scheduled, null, 1, Now, Now));
        Assert.Throws<ArgumentException>(() => new Interview(
            1, 1, "tech", slot, "UTC", [7, 7], "Room", null, InterviewStatus.Scheduled, null, 1, Now, Now));
    }

    private static InterviewWrite ValidWrite(
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null,
        string? location = "Room A",
        string? meetingUrl = null) => new(
        ApplicationId: 10,
        InterviewType: "tech",
        StartsAt: startsAt ?? Tomorrow,
        EndsAt: endsAt ?? Tomorrow.AddHours(1),
        Timezone: "Asia/Ho_Chi_Minh",
        InterviewerUserIds: [7, 8],
        Location: location,
        MeetingUrl: meetingUrl);

    internal static Interview CreateInterview(
        InterviewStatus status,
        long version,
        DateTimeOffset? startsAt = null,
        long id = 42)
    {
        var start = startsAt ?? Tomorrow;
        return new Interview(
            id,
            applicationId: 10,
            interviewType: "tech",
            new InterviewSlot(start, start.AddHours(1)),
            "Asia/Ho_Chi_Minh",
            [7, 8],
            "Room A",
            null,
            status,
            cancellationReason: null,
            version,
            createdAt: Now.AddDays(-2),
            updatedAt: Now.AddMinutes(-5));
    }
}

using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Evaluations;

public sealed class BlindEvaluationPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly EvaluationInterview Interview = new(5, InterviewStatus.Completed, [7, 8]);

    private static readonly Evaluation User7Version1 = EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7);
    private static readonly Evaluation User8Version1 = EvaluationTests.CreateEvaluation(id: 2, version: 1, evaluatorUserId: 8);
    private static readonly Evaluation User8Version2 = EvaluationTests.CreateEvaluation(id: 3, version: 2, evaluatorUserId: 8);

    [Fact]
    public void Apply_ReadAllPermission_SeesEveryVersionEvenWhenNotPanelist()
    {
        var visible = BlindEvaluationPolicy.Apply(Actor(99, EvaluationPermissions.ReadAll), Interview, [User8Version2, User7Version1, User8Version1]);

        Assert.Equal([1L, 2L, 3L], visible.Select(e => e.Id));
    }

    [Fact]
    public void Apply_NonPanelistWithoutReadAll_ThrowsForbiddenBlindPolicy()
    {
        var exception = Assert.Throws<CoreHrForbiddenException>(() =>
            BlindEvaluationPolicy.Apply(Actor(99, EvaluationPermissions.Read), Interview, [User7Version1]));

        Assert.Equal(BlindEvaluationPolicy.BlindPolicyCode, exception.Code);
    }

    [Fact]
    public void Apply_PanelistWithoutOwnSubmission_SeesNothing()
    {
        var visible = BlindEvaluationPolicy.Apply(Actor(7, EvaluationPermissions.Read), Interview, [User8Version1, User8Version2]);

        Assert.Empty(visible);
    }

    [Fact]
    public void Apply_PanelistWithLockedSubmission_SeesOwnVersionsAndOthersLatestOnly()
    {
        var visible = BlindEvaluationPolicy.Apply(Actor(7, EvaluationPermissions.Read), Interview, [User8Version1, User8Version2, User7Version1]);

        Assert.Equal([1L, 3L], visible.Select(e => e.Id));
    }

    [Fact]
    public void Apply_PanelistWhoseLatestIsUnlocked_SeesOnlyOwnRows()
    {
        var user7Unlocked = EvaluationTests.CreateEvaluation(id: 4, version: 2, evaluatorUserId: 7, unlockedAt: Now);

        var visible = BlindEvaluationPolicy.Apply(
            Actor(7, EvaluationPermissions.Read),
            Interview,
            [User8Version2, User7Version1, user7Unlocked]);

        Assert.Equal([1L, 4L], visible.Select(e => e.Id));
    }

    [Fact]
    public void Apply_PanelistSeesOwnHistoryButNotOthersHistory()
    {
        var user7Unlocked = EvaluationTests.CreateEvaluation(id: 4, version: 2, evaluatorUserId: 7, unlockedAt: Now);
        var user7Resubmitted = EvaluationTests.CreateEvaluation(id: 5, version: 3, evaluatorUserId: 7);

        var visible = BlindEvaluationPolicy.Apply(
            Actor(7, EvaluationPermissions.Read),
            Interview,
            [User8Version1, User8Version2, User7Version1, user7Unlocked, user7Resubmitted]);

        Assert.Equal([1L, 4L, 5L, 3L], visible.Select(e => e.Id));
    }

    private static CoreHrActor Actor(long userId, params string[] permissions) => new(
        userId,
        EmployeeId: null,
        CoreHrDataScope.Self,
        permissions.ToHashSet(StringComparer.Ordinal),
        "test-correlation");
}

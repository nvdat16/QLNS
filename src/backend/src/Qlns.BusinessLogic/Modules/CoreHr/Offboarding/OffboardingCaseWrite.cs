namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>Client payload of OpenAPI OffboardingCaseWrite. Validated by <see cref="OffboardingCase.Open"/>.</summary>
public sealed record OffboardingCaseWrite(
    long EmployeeId,
    string? SeparationType,
    DateOnly? NoticeReceivedOn,
    DateOnly LastWorkingDate,
    long? HandoverToEmployeeId,
    string? Reason);

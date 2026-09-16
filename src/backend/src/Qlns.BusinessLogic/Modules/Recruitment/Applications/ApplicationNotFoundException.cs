namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed class ApplicationNotFoundException(long applicationId)
    : Exception($"Recruitment application {applicationId} was not found.");

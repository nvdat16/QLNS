namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed class ConcurrencyConflictException()
    : Exception("The recruitment application changed concurrently.");

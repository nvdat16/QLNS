namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

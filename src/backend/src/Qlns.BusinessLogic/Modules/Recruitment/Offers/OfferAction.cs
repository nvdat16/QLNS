namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>Internal workflow action of <c>POST /recruitment/offers/{offerId}/{action}</c>.</summary>
public enum OfferAction
{
    Approve,
    Send,
    Extend,
    Cancel
}

public static class OfferActionNames
{
    public static string ToContract(this OfferAction action) => action switch
    {
        OfferAction.Approve => "approve",
        OfferAction.Send => "send",
        OfferAction.Extend => "extend",
        OfferAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out OfferAction action)
    {
        action = value switch
        {
            "approve" => OfferAction.Approve,
            "send" => OfferAction.Send,
            "extend" => OfferAction.Extend,
            "cancel" => OfferAction.Cancel,
            _ => default
        };

        return value is "approve" or "send" or "extend" or "cancel";
    }
}

/// <summary>Candidate decision of <c>POST /recruitment/offers/{offerId}/response</c>.</summary>
public enum OfferDecision
{
    Accept,
    Decline
}

public static class OfferDecisionNames
{
    public static string ToContract(this OfferDecision decision) => decision switch
    {
        OfferDecision.Accept => "accept",
        OfferDecision.Decline => "decline",
        _ => throw new ArgumentOutOfRangeException(nameof(decision))
    };

    public static bool TryParseContract(string? value, out OfferDecision decision)
    {
        decision = value switch
        {
            "accept" => OfferDecision.Accept,
            "decline" => OfferDecision.Decline,
            _ => default
        };

        return value is "accept" or "decline";
    }
}

/// <summary>
/// Every persisted status change of an offer other than acceptance (which has its own handoff transaction).
/// Drives the audit action name and, for <see cref="Send"/> / <see cref="Extend"/>, the outbox notification.
/// </summary>
public enum OfferTransition
{
    Approve,
    Send,
    Extend,
    Cancel,
    Expire,
    Decline
}

public static class OfferTransitionNames
{
    public static string ToAuditAction(this OfferTransition transition) => transition switch
    {
        OfferTransition.Approve => "recruitment.offer.approve",
        OfferTransition.Send => "recruitment.offer.send",
        OfferTransition.Extend => "recruitment.offer.extend",
        OfferTransition.Cancel => "recruitment.offer.cancel",
        OfferTransition.Expire => "recruitment.offer.expire",
        OfferTransition.Decline => "recruitment.offer.decline",
        _ => throw new ArgumentOutOfRangeException(nameof(transition))
    };
}

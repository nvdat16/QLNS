namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

/// <summary>Kanban stage of an application (applications.stage). Six active stages in order, then two terminal ones.</summary>
public enum ApplicationStage
{
    SourcedApplied,
    AiScreening,
    TechInterview,
    ExecutiveRound,
    OfferLetter,
    HiredReady,
    Rejected,
    Withdrawn
}

public static class ApplicationStageNames
{
    /// <summary>The six active stages in pipeline order (REC-03.1); advance moves exactly one step along this list.</summary>
    public static IReadOnlyList<ApplicationStage> ActiveStages { get; } =
    [
        ApplicationStage.SourcedApplied,
        ApplicationStage.AiScreening,
        ApplicationStage.TechInterview,
        ApplicationStage.ExecutiveRound,
        ApplicationStage.OfferLetter,
        ApplicationStage.HiredReady
    ];

    public static bool IsActive(this ApplicationStage stage) => ActiveStages.Contains(stage);

    public static string ToContract(this ApplicationStage stage) => stage switch
    {
        ApplicationStage.SourcedApplied => "sourced_applied",
        ApplicationStage.AiScreening => "ai_screening",
        ApplicationStage.TechInterview => "tech_interview",
        ApplicationStage.ExecutiveRound => "executive_round",
        ApplicationStage.OfferLetter => "offer_letter",
        ApplicationStage.HiredReady => "hired_ready",
        ApplicationStage.Rejected => "rejected",
        ApplicationStage.Withdrawn => "withdrawn",
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };

    public static bool TryParseContract(string value, out ApplicationStage stage)
    {
        stage = value switch
        {
            "sourced_applied" => ApplicationStage.SourcedApplied,
            "ai_screening" => ApplicationStage.AiScreening,
            "tech_interview" => ApplicationStage.TechInterview,
            "executive_round" => ApplicationStage.ExecutiveRound,
            "offer_letter" => ApplicationStage.OfferLetter,
            "hired_ready" => ApplicationStage.HiredReady,
            "rejected" => ApplicationStage.Rejected,
            "withdrawn" => ApplicationStage.Withdrawn,
            _ => default
        };

        return value is "sourced_applied" or "ai_screening" or "tech_interview" or
            "executive_round" or "offer_letter" or "hired_ready" or "rejected" or "withdrawn";
    }
}

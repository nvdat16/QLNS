namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>Create/replace payload for a position definition (OpenAPI <c>PositionWrite</c>).</summary>
public sealed record PositionWrite(
    string Code,
    string Name,
    string? Level,
    string? Description);

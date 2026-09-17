namespace Qlns.BusinessLogic.Modules.CoreHr.Shared;

/// <summary>Validated 1-based page request. Matches the OpenAPI Page/PageSize parameters.</summary>
public sealed record PageRequest
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    public int Page { get; }
    public int PageSize { get; }

    public PageRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "page must be at least 1.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"pageSize must be between 1 and {MaxPageSize}.");
        }

        Page = page;
        PageSize = pageSize;
    }

    public static PageRequest Default { get; } = new();

    public int Skip => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalItems)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public static PagedResult<T> Empty(PageRequest request) => new([], request.Page, request.PageSize, 0);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalItems);
}

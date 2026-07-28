namespace CampaignUserService.Application.Common.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) =>
        new()
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
}

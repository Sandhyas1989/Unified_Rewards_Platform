using Microsoft.EntityFrameworkCore;

namespace UnifiedRewards.Application.Common.Models;

/// <summary>A single page of results plus the metadata a client needs to page through the rest.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

/// <summary>
/// Normalises caller-supplied paging input: defaults to page 1 / 25 per page and caps the page
/// size so a single request can never pull an unbounded result set.
/// </summary>
public readonly struct PageRequest
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;

    public int Page { get; }
    public int PageSize { get; }

    public PageRequest(int? page, int? pageSize)
    {
        Page = page is { } p && p > 0 ? p : 1;
        var size = pageSize is { } s && s > 0 ? s : DefaultPageSize;
        PageSize = Math.Min(size, MaxPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}

public static class PagingExtensions
{
    /// <summary>Counts the query, then returns just the requested page — never the whole table.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageRequest page, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}

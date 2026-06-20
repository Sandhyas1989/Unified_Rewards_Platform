namespace UnifiedRewards.Shared;

/// <summary>
/// Generic paged-result envelope returned by paginated API endpoints across all services.
/// Single definition here eliminates the duplicate PagedResult&lt;T&gt; that lived in each service's Contracts.cs.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

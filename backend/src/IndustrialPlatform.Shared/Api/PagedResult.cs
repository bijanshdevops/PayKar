namespace IndustrialPlatform.Shared.Api;

/// <summary>ساختار پاسخ صفحه‌بندی‌شده طبق سند 04-Api-Contract.md بخش ۳.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}

/// <summary>پارامترهای ورودی استاندارد صفحه‌بندی/مرتب‌سازی (سند 04، بخش ۳).</summary>
public sealed class PageRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? 20 : Math.Min(value, MaxPageSize);
    }

    public string SortBy { get; set; } = "createdAtUtc";
    public string SortDir { get; set; } = "desc";
}

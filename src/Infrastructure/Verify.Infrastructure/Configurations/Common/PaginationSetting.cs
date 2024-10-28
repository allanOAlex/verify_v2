namespace Verify.Infrastructure.Configurations.Common;
internal sealed record PaginationSetting
{
    public int DefaultPageSize { get; init; } = 50;
    public int MaxPageSize { get; init; } = 100; // optional max limit
    public bool IsLastPage { get; init; } = false;
    public bool IsFirstPage { get; init; } = false;

    public int? PreviousPage { get; init; } = 0;
    public int? CurrentPage { get; init; } = 0;
    public int? NextPage { get; init; } = 0;

    public int? Cursor { get; init; } = 0;
    public int? PreviousCursor { get; init; }
    public int? NextCursor { get; init; }
    public int? LastCursor { get; init; }

    public bool IsSearchMode { get; init; } = false;
}

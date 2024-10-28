namespace Verify.Infrastructure.Configurations.Common;
internal sealed class SearchSetting<T>
{
    public string? SearchParam { get; init; }
    public T? SearchValue { get; init; }
    public PaginationSetting? PaginationSetting { get; init; } = new();
}

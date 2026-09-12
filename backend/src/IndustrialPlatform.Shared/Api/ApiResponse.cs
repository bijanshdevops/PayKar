namespace IndustrialPlatform.Shared.Api;

/// <summary>
/// قرارداد پاسخ استاندارد API طبق CLAUDE.md و سند 04-Api-Contract.md.
/// تمام اندپوینت‌ها — موفق یا ناموفق — این ساختار را برمی‌گردانند.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string? message = null, int statusCode = 200) => new()
    {
        Success = true,
        StatusCode = statusCode,
        Message = message,
        Data = data,
        Errors = null
    };

    public static ApiResponse<T> Fail(string message, int statusCode, IDictionary<string, string[]>? errors = null) => new()
    {
        Success = false,
        StatusCode = statusCode,
        Message = message,
        Data = default,
        Errors = errors
    };
}

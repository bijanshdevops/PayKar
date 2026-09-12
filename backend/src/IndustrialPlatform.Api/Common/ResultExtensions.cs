using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Api.Common;

/// <summary>
/// نگاشت خروجی Result/Result&lt;T&gt; لایه Application/Identity به پاکت استاندارد ApiResponse&lt;T&gt;
/// طبق سند 04-Api-Contract.md. تمام Endpointهای Minimal API باید از این Extension استفاده کنند
/// تا هرگز مستقیماً Exception یا شیء خام برنگردانند.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToApiResult<T>(this Result<T> result, string? successMessage = null, int successStatusCode = 200)
    {
        if (result.IsSuccess)
        {
            return Results.Json(ApiResponse<T>.Ok(result.Value, successMessage, successStatusCode), statusCode: successStatusCode);
        }

        return ToFailureResult<T>(result.Error);
    }

    public static IResult ToApiResult(this Result result, string? successMessage = null, int successStatusCode = 200)
    {
        if (result.IsSuccess)
        {
            return Results.Json(ApiResponse<object>.Ok(null, successMessage, successStatusCode), statusCode: successStatusCode);
        }

        return ToFailureResult<object>(result.Error);
    }

    private static IResult ToFailureResult<T>(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => error.Code == "RATE_LIMIT_EXCEEDED" ? StatusCodes.Status429TooManyRequests : StatusCodes.Status400BadRequest
        };

        var response = ApiResponse<T>.Fail(
            error.Message,
            statusCode,
            new Dictionary<string, string[]> { ["code"] = new[] { error.Code } });

        return Results.Json(response, statusCode: statusCode);
    }
}

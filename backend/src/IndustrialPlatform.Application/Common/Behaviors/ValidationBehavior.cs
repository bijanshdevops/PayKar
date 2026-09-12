using FluentValidation;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior که FluentValidation را قبل از رسیدن Command/Query به Handler اجرا می‌کند
/// و در صورت خطا، بدون پرتاب Exception، یک Result ناموفق برمی‌گرداند (طبق CLAUDE.md).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(request, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var message = string.Join(" ", failures.Select(f => f.ErrorMessage));
        var error = Error.Validation("VALIDATION_ERROR", message);

        // ساخت Result<T> ناموفق به‌صورت Reflection-Free با بررسی نوع پاسخ
        var resultType = typeof(TResponse);
        if (resultType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
            .MakeGenericMethod(resultType.GetGenericArguments()[0]);

        return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
    }
}

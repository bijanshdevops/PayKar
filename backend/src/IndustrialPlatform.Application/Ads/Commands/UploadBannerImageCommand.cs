using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>آپلود تصویر بنر تبلیغاتی — طبق ADR-008 (عیناً الگوی UploadCompanyDocumentCommand).</summary>
public sealed record UploadBannerImageCommand(Stream FileContent, string FileName) : IRequest<Result<string>>;

public sealed class UploadBannerImageCommandHandler : IRequestHandler<UploadBannerImageCommand, Result<string>>
{
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;

    public UploadBannerImageCommandHandler(IFileStorageService fileStorage, ICurrentUserService currentUser)
    {
        _fileStorage = fileStorage;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(UploadBannerImageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<string>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var fileUrl = await _fileStorage.SaveAsync(request.FileContent, request.FileName, "banner-ads", cancellationToken);
        return Result.Success(fileUrl);
    }
}

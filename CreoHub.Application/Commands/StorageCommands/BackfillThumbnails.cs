using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record BackfillThumbnailsCommand(Guid ShopId) : IRequest<BaseResponse<BackfillThumbnailsResultDTO>>;

public class BackfillThumbnailsResultDTO
{
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BackfillThumbnailsHandler
    : IRequestHandler<BackfillThumbnailsCommand, BaseResponse<BackfillThumbnailsResultDTO>>
{
    private readonly IMediaProductRepository _mediaProductRepository;
    private readonly IThumbnailGenerationService _thumbnailService;

    public BackfillThumbnailsHandler(
        IMediaProductRepository mediaProductRepository,
        IThumbnailGenerationService thumbnailService)
    {
        _mediaProductRepository = mediaProductRepository;
        _thumbnailService = thumbnailService;
    }

    public async Task<BaseResponse<BackfillThumbnailsResultDTO>> Handle(
        BackfillThumbnailsCommand request, CancellationToken cancellationToken)
    {
        var result = new BackfillThumbnailsResultDTO();

        try
        {
            var candidates = await _mediaProductRepository
                .GetVideosWithoutThumbnailAsync(request.ShopId);

            result.Total = candidates.Count;

            foreach (var mediaProduct in candidates)
            {
                try
                {
                    await _thumbnailService.GenerateAsync(
                        mediaProduct.StorageObjectId, cancellationToken);
                    result.Succeeded++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(
                        $"StorageObject {mediaProduct.StorageObjectId}: {ex.Message}");
                }
            }

            return BaseResponse<BackfillThumbnailsResultDTO>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<BackfillThumbnailsResultDTO>.Fail(ex.Message);
        }
    }
}

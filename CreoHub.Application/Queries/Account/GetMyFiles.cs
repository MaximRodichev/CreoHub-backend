using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Account;

public record GetMyFilesQuery(Guid UserId) : IRequest<BaseResponse<List<MyFilesProductGroupDTO>>>;

public class GetMyFilesHandler
    : IRequestHandler<GetMyFilesQuery, BaseResponse<List<MyFilesProductGroupDTO>>>
{
    private readonly IContentAccessRepository _contentAccessRepository;
    private readonly IStorageService _storageService;

    public GetMyFilesHandler(IContentAccessRepository contentAccessRepository, IStorageService storageService)
    {
        _contentAccessRepository = contentAccessRepository;
        _storageService = storageService;
    }

    public async Task<BaseResponse<List<MyFilesProductGroupDTO>>> Handle(
        GetMyFilesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var accesses = await _contentAccessRepository.GetUserFilesAsync(request.UserId);

            var groups = accesses
                .GroupBy(ca => ca.ContentFile.ProductId)
                .Select(g =>
                {
                    var product = g.First().ContentFile.Product;

                    // Первый медиа-продукт → signed URL превью для карточки
                    var rawPreviewKey = product.MediaProducts
                        .OrderBy(m => m.SortOrder)
                        .FirstOrDefault()?.StorageObject?.Key;
                    var previewKey = rawPreviewKey != null
                        ? _storageService.GeneratePresignedUrl(rawPreviewKey, 60)
                        : null;

                    return new MyFilesProductGroupDTO
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        PreviewKey = previewKey,
                        Files = g.Select(ca => new MyPurchasedFileDTO
                        {
                            ContentFileId = ca.ContentFileId,
                            FileName = ca.ContentFile.PreviewName,
                            OrderId = ca.OrderId,
                            GrantedAt = ca.GrantedAt
                        }).ToList()
                    };
                })
                .ToList();

            return BaseResponse<List<MyFilesProductGroupDTO>>.Success(groups);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<MyFilesProductGroupDTO>>.Fail(ex.Message);
        }
    }
}

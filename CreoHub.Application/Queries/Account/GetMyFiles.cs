using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Account;

public record GetMyFilesQuery(Guid UserId) : IRequest<BaseResponse<List<MyFilesProductGroupDTO>>>;

public class GetMyFilesHandler
    : IRequestHandler<GetMyFilesQuery, BaseResponse<List<MyFilesProductGroupDTO>>>
{
    private readonly IContentAccessRepository _contentAccessRepository;

    public GetMyFilesHandler(IContentAccessRepository contentAccessRepository)
    {
        _contentAccessRepository = contentAccessRepository;
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

                    // Первый медиа-продукт → ключ превью для карточки
                    var previewKey = product.MediaProducts
                        .OrderBy(m => m.SortOrder)
                        .FirstOrDefault()?.StorageObjectId.ToString();

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

using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsByFilterQuery(
    FiltersDto filters,
    Guid?      UserId    = null,
    string?    SessionId = null
) : IRequest<BaseResponse<PageViewDTO>>;

public class GetProductsByFilterHandler : IRequestHandler<GetProductsByFilterQuery, BaseResponse<PageViewDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService    _storageService;
    private readonly IEventTracker      _events;

    public GetProductsByFilterHandler(
        IProductRepository productRepository,
        IStorageService    storageService,
        IEventTracker      events)
    {
        _productRepository = productRepository;
        _storageService    = storageService;
        _events            = events;
    }

    public async Task<BaseResponse<PageViewDTO>> Handle(GetProductsByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            (IReadOnlyList<ProductViewDTO> products, int count) = await _productRepository.GetProductsByFilters(request.filters);

            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(p.PreviewKey))
                    p.PreviewKey = _storageService.GeneratePresignedUrl(p.PreviewKey, 60);
                if (!string.IsNullOrEmpty(p.PreviewThumbnailKey))
                    p.PreviewThumbnailKey = _storageService.GeneratePresignedUrl(p.PreviewThumbnailKey, 60);
            }

            // Track search events only when a search query is present
            var q = request.filters.Search;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var eventType = count == 0 ? EventTypes.SearchNoResults : EventTypes.Search;
                _events.Track(eventType,
                    userId:    request.UserId,
                    sessionId: request.SessionId,
                    payload:   new { q = q.Trim().ToLowerInvariant() });
            }

            PageViewDTO page = new PageViewDTO()
            {
                Products = products,
                CountProducts = count
            };
            return BaseResponse<PageViewDTO>.Success(page);
        }
        catch (Exception ex)
        {
            return BaseResponse<PageViewDTO>.Fail(ex.Message);
        }
    }
}
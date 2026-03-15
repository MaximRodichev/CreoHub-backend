using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsByFilterQuery(FiltersDto filters) : IRequest<BaseResponse<PageViewDTO>>
{
    
}

public class GetProductsByFilterHandler : IRequestHandler<GetProductsByFilterQuery, BaseResponse<PageViewDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductsByFilterHandler(IProductRepository ProductRepository,  IMapper Mapper)
    {
        _productRepository = ProductRepository;
        _mapper = Mapper;
    }
    
    public async Task<BaseResponse<PageViewDTO>> Handle(GetProductsByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            (IReadOnlyList<ProductViewDTO> products, int count) = await _productRepository.GetProductsByFilters(request.filters);
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
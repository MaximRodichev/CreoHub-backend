using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsByFilterQuery(FiltersDto filters) : IRequest<BaseResponse<IReadOnlyList<ProductViewDTO>>>

{
    
}

public class GetProductsByFilterHandler : IRequestHandler<GetProductsByFilterQuery, BaseResponse<IReadOnlyList<ProductViewDTO>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductsByFilterHandler(IProductRepository ProductRepository,  IMapper Mapper)
    {
        _productRepository = ProductRepository;
        _mapper = Mapper;
    }
    
    public async Task<BaseResponse<IReadOnlyList<ProductViewDTO>>> Handle(GetProductsByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ProductViewDTO> products = await _productRepository.GetProductsByFilters(request.filters);
            return BaseResponse<IReadOnlyList<ProductViewDTO>>.Success(products);
        }
        catch (Exception ex)
        {
            return BaseResponse<IReadOnlyList<ProductViewDTO>>.Fail(ex.Message);
        }
    }
}
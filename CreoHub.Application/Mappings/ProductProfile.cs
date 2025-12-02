using AutoMapper;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Mappings;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductViewDTO>();
        
    }

}
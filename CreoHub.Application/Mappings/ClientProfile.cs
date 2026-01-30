using AutoMapper;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Mappings;

public class ClientProfile : Profile
{
    public ClientProfile()
    {
        CreateMap<ClientShortInfoDTO, ClientNameDTO>().ReverseMap();
    }
}
using AutoMapper;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Mappings;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<AuthAccountDTO, User>().ReverseMap();
        CreateMap<IdentityDTO, User>().ReverseMap();
    }
}
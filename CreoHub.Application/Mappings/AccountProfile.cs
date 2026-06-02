using AutoMapper;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Mappings;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<AuthAccountDTO, User>().ReverseMap();
        CreateMap<User, IdentityDTO>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
}
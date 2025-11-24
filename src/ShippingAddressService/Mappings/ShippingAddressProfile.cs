using AutoMapper;
using Loft.Common.DTOs;
using ShippingAddressService.Entities;

namespace ShippingAddressService.Mappings;

public class ShippingAddressProfile : Profile
{
    public ShippingAddressProfile()
    {
        CreateMap<ShippingAddress, ShippingAddressDTO>();
        
        CreateMap<ShippingAddressCreateDTO, ShippingAddress>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore());
        
        CreateMap<ShippingAddressUpdateDTO, ShippingAddress>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.IsDefault, opt => opt.Condition(src => src.IsDefault.HasValue))
            .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault!.Value));
    }
}
using AutoMapper;
using Loft.Common.DTOs;
using ShippingAddressService.Entities;

namespace ShippingAddressService.Mappings;

public class ShippingAddressProfile : Profile
{
    public ShippingAddressProfile()
    {
        CreateMap<ShippingAddress, ShippingAddressDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.PostalCode))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country));
    }
}
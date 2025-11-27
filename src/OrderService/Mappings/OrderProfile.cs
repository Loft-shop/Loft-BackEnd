using AutoMapper;
using Loft.Common.DTOs;
using OrderService.Entities;

namespace OrderService.Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDTO>()
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => 
                src.ShippingAddressId.HasValue 
                    ? new ShippingAddressDTO(
                        src.ShippingAddressId.Value,
                        src.CustomerId,
                        src.ShippingAddress ?? "",
                        src.ShippingCity ?? "",
                        src.ShippingPostalCode ?? "",
                        src.ShippingCountry ?? "",
                        src.ShippingRecipientName,
                        false,
                        null)
                    : null));

        CreateMap<OrderItem, OrderItemDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderId))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName));
    }
}
using AutoMapper;
using Loft.Common.DTOs;
using OrderService.Entities;

namespace OrderService.Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderItem, OrderItemDTO>();

        CreateMap<Order, OrderDTO>()
           .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
           .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src =>
               src.ShippingAddressId == null
                   ? null
                   : new ShippingAddressDTO
                   {
                       Id = src.ShippingAddressId.Value,
                       CustomerId = src.CustomerId,
                       Address = src.ShippingAddress ?? "",
                       City = src.ShippingCity ?? "",
                       PostalCode = src.ShippingPostalCode ?? "",
                       Country = src.ShippingCountry ?? "",
                       RecipientName = src.ShippingRecipientName,
                       IsDefault = false,
                       CreatedAt = null
                   }
           ));
    }
}

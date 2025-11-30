using AutoMapper;
using Loft.Common.DTOs;
using PaymentService.Entities;
//
namespace PaymentService.Mappings;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<Payment, PaymentDTO>();
        CreateMap<PaymentDTO, Payment>();
        CreateMap<CreatePaymentDTO, Payment>();
    }
}
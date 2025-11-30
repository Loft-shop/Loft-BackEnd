using Loft.Common.DTOs;
//
namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDTO> CreatePaymentAsync(CreatePaymentDTO dto);
    Task<PaymentDTO> ConfirmPaymentAsync(long paymentId);
    Task<PaymentDTO> GetPaymentByIdAsync(long paymentId);
    Task<IEnumerable<PaymentDTO>> GetPaymentsByOrderIdAsync(long orderId);
    Task<PaymentDTO> RefundPaymentAsync(long paymentId);
}
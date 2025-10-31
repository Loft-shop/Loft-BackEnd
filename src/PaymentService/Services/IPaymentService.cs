using Loft.Common.DTOs;
using Loft.Common.Enums;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDTO> CreatePayment(PaymentDTO payment);
    Task<PaymentDTO> ProcessPayment(long orderId,PaymentMethod method,string? providerData = null);
    Task<PaymentDTO?> GetPaymentById(long paymentId);
    Task<PaymentDTO?> GetPaymentByOrderId(long orderId);
    Task<PaymentDTO> RefundPayment(long paymentId);
    Task<PaymentStatus> GetPaymentStatus(long paymentId);
    
    /*
     * Примечания: providerData — произвольная строка/JSON от платежного провайдера;
     * ProcessPayment возвращает PaymentDTO с заполненным статусом.
     */
}
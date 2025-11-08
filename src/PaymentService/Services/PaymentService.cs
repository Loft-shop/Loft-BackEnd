using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Data;
using PaymentService.Entities;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(PaymentDbContext context, IMapper mapper, ILogger<PaymentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaymentDTO> CreatePayment(PaymentDTO payment)
    {
        var entity = _mapper.Map<Payment>(payment);
        entity.PaymentDate = DateTime.UtcNow;
        _context.Payments.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<PaymentDTO>(entity);
    }

    public async Task<PaymentDTO> ProcessPayment(long orderId, PaymentMethod method, string? providerData = null)
    {
        // Попытка найти уже существующий платеж по заказу
        var existing = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (existing != null)
        {
            return _mapper.Map<PaymentDTO>(existing);
        }

        // В реальном приложении тут бы вызывался провайдер оплаты
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = 0m, // неизвестно — в реальном кейсе можно запросить сумму у OrderService
            Method = method,
            PaymentDate = DateTime.UtcNow
        };

        // Простая имитация: если метод не CASH_ON_DELIVERY — считаем успешным
        if (method == PaymentMethod.CASH_ON_DELIVERY)
        {
            payment.Status = PaymentStatus.PEDDING;
        }
        else
        {
            payment.Status = PaymentStatus.COMPLETED;
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Processed payment for order {OrderId} with status {Status}", orderId, payment.Status);
        return _mapper.Map<PaymentDTO>(payment);
    }

    public async Task<PaymentDTO?> GetPaymentById(long paymentId)
    {
        var p = await _context.Payments.FindAsync(paymentId);
        return p == null ? null : _mapper.Map<PaymentDTO>(p);
    }

    public async Task<PaymentDTO?> GetPaymentByOrderId(long orderId)
    {
        var p = await _context.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId);
        return p == null ? null : _mapper.Map<PaymentDTO>(p);
    }

    public async Task<PaymentDTO> RefundPayment(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        payment.Status = PaymentStatus.REFUNDED;
        await _context.SaveChangesAsync();
        return _mapper.Map<PaymentDTO>(payment);
    }

    public async Task<PaymentStatus> GetPaymentStatus(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");
        return payment.Status;
    }
}
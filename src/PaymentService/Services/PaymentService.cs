using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Entities;
using PaymentService.Services.Providers;
//
namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly PaymentProviderFactory _providerFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentDbContext context,
        PaymentProviderFactory providerFactory,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaymentDTO> CreatePaymentAsync(CreatePaymentDTO dto)
    {
        _logger.LogInformation("Creating payment for order {OrderId} with method {Method}", 
            dto.OrderId, dto.Method);

        // Получаем провайдер для выбранного метода
        var provider = _providerFactory.GetProvider(dto.Method);
        
        // Создаем транзакцию через провайдер
        var transactionId = await provider.CreatePaymentAsync(dto.Amount, dto.OrderId);

        // Создаем запись в БД
        var payment = new Payment
        {
            OrderId = dto.OrderId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = dto.Method == PaymentMethod.CASH_ON_DELIVERY 
                ? PaymentStatus.PENDING 
                : PaymentStatus.REQUIRES_CONFIRMATION,
            PaymentDate = DateTime.UtcNow,
            TransactionId = transactionId
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment created with ID {PaymentId}, TransactionId {TransactionId}", 
            payment.Id, transactionId);

        return _mapper.Map<PaymentDTO>(payment);
    }

    public async Task<PaymentDTO> ConfirmPaymentAsync(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        if (payment.Status == PaymentStatus.COMPLETED)
        {
            _logger.LogWarning("Payment {PaymentId} already completed", paymentId);
            return _mapper.Map<PaymentDTO>(payment);
        }

        // Получаем провайдер и подтверждаем платеж
        var provider = _providerFactory.GetProvider(payment.Method);
        var success = await provider.ConfirmPaymentAsync(payment.TransactionId!);

        if (success)
        {
            payment.Status = PaymentStatus.COMPLETED;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment {PaymentId} confirmed successfully", paymentId);
        }

        return _mapper.Map<PaymentDTO>(payment);
    }

    public async Task<PaymentDTO> GetPaymentByIdAsync(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        return _mapper.Map<PaymentDTO>(payment);
    }

    public async Task<IEnumerable<PaymentDTO>> GetPaymentsByOrderIdAsync(long orderId)
    {
        var payments = await _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PaymentDTO>>(payments);
    }

    public async Task<PaymentDTO> RefundPaymentAsync(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        if (payment.Status != PaymentStatus.COMPLETED)
            throw new InvalidOperationException("Can only refund completed payments");

        // Получаем провайдер и делаем возврат
        var provider = _providerFactory.GetProvider(payment.Method);
        var success = await provider.RefundPaymentAsync(payment.TransactionId!);

        if (success)
        {
            payment.Status = PaymentStatus.REFUNDED;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment {PaymentId} refunded successfully", paymentId);
        }

        return _mapper.Map<PaymentDTO>(payment);
    }
}
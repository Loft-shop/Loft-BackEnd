using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;

    public OrderService(OrderDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<OrderDTO> CreateOrder(OrderDTO orderDto, IEnumerable<OrderItemDTO> items)
    {
        var itemsList = items.ToList();
        var order = new Order
        {
            CustomerId = orderDto.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.PENDING,
            TotalAmount = itemsList.Sum(i => i.Quantity * i.Price),
            UpdatedDate = DateTime.UtcNow,
            OrderItems = itemsList.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return _mapper.Map<OrderDTO>(order);
    }

    public async Task<OrderDTO?> GetOrderById(long orderId)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
        return order == null ? null : _mapper.Map<OrderDTO>(order);
    }

    public async Task<IEnumerable<OrderDTO>> GetOrdersByCustomerId(long customerId, int page = 1, int pageSize = 20)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return _mapper.Map<IEnumerable<OrderDTO>>(orders);
    }

    public async Task UpdateOrderStatus(long orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            order.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CancelOrder(long orderId)
    {
        await UpdateOrderStatus(orderId, OrderStatus.CANCELED);
    }

    public async Task AddOrderItems(long orderId, OrderItemDTO itemDto)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order != null)
        {
            var item = new OrderItem
            {
                OrderId = orderId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                Price = itemDto.Price
            };
            order.OrderItems.Add(item);
            order.TotalAmount += item.Quantity * item.Price;
            order.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveOrderItems(long orderId, long orderItemId)
    {
        var item = await _context.OrderItems.FindAsync(orderItemId);
        if (item != null && item.OrderId == orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.TotalAmount -= item.Quantity * item.Price;
                order.UpdatedDate = DateTime.UtcNow;
            }
            _context.OrderItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public Task<decimal> CalculateOrderTotal(IEnumerable<OrderItemDTO> items)
    {
        return Task.FromResult(items.Sum(i => i.Quantity * i.Price));
    }

    public async Task<IEnumerable<OrderDTO>> GetAllOrders()
    {
        var orders = await _context.Orders.Include(o => o.OrderItems).ToListAsync();
        return _mapper.Map<IEnumerable<OrderDTO>>(orders);
    }
}
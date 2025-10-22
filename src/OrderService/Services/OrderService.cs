using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Data;
using OrderService.Entities;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderDbContext context, IMapper mapper, IHttpClientFactory httpClientFactory, ILogger<OrderService> logger)
    {
        _context = context;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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

    public async Task<OrderDTO?> CheckoutFromCart(long customerId)
    {
        // 1. Получаем данные покупателя из UserService
        var userClient = _httpClientFactory.CreateClient("UserService");
        string? customerName = null;
        string? customerEmail = null;
        
        try
        {
            var userResponse = await userClient.GetAsync($"/api/users/{customerId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var userContent = await userResponse.Content.ReadAsStringAsync();
                var user = await userResponse.Content.ReadFromJsonAsync<UserDto>();
                if (user != null)
                {
                    customerName = user.Username ?? user.Email;
                    customerEmail = user.Email;
                }
            }
        }
        catch (Exception ex)
        {
            // Если не удалось получить данные пользователя, продолжаем без них
            _logger.LogWarning(ex, $"Failed to load user details for customer {customerId}");
        }

        // 2. Получаем корзину пользователя из CartService
        var cartClient = _httpClientFactory.CreateClient("CartService");
        var cartResponse = await cartClient.GetAsync($"/api/carts/customer/{customerId}");
        
        if (!cartResponse.IsSuccessStatusCode)
        {
            throw new Exception("Не удалось получить корзину пользователя");
        }

        var cart = await cartResponse.Content.ReadFromJsonAsync<CartDTO>();
        if (cart == null)
        {
            throw new Exception("Корзина пуста");
        }

        // 3. Получаем товары из корзины
        var cartItemsResponse = await cartClient.GetAsync($"/api/carts/{cart.Id}/items");
        if (!cartItemsResponse.IsSuccessStatusCode)
        {
            throw new Exception("Не удалось получить товары из корзины");
        }

        var cartItems = await cartItemsResponse.Content.ReadFromJsonAsync<List<CartItemDTO>>();
        if (cartItems == null || !cartItems.Any())
        {
            throw new Exception("Корзина пуста");
        }

        // 4. Получаем цены товаров из ProductService
        var productClient = _httpClientFactory.CreateClient("ProductService");
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var cartItem in cartItems)
        {
            var productResponse = await productClient.GetAsync($"/api/products/{cartItem.ProductId}");
            if (!productResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Товар с ID {cartItem.ProductId} не найден");
            }

            var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
            if (product == null)
            {
                throw new Exception($"Товар с ID {cartItem.ProductId} не найден");
            }

            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Price = product.Price
            };

            orderItems.Add(orderItem);
            totalAmount += orderItem.Quantity * orderItem.Price;
        }

        // 5. Создаём заказ с данными покупателя
        var order = new Order
        {
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.PENDING,
            TotalAmount = totalAmount,
            UpdatedDate = DateTime.UtcNow,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 6. Очищаем корзину после успешного создания заказа
        await cartClient.DeleteAsync($"/api/carts/{customerId}");

        return _mapper.Map<OrderDTO>(order);
    }
}

// DTO для пользователя
public class UserDto
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}

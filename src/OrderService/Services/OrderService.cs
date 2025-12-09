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
        return await CreateOrderWithShipping(orderDto, items, null, null);
    }

    public async Task<OrderDTO> CreateOrderWithShipping(OrderDTO orderDto, IEnumerable<OrderItemDTO> items, long? shippingAddressId = null, ShippingAddressDTO? customShippingAddress = null)
    {
        var itemsList = items.ToList();
        
        // =============================================================================
        // НОВАЯ СИСТЕМА: Загружаем информацию о товарах для сохранения в заказе
        // =============================================================================
        var enrichedItems = new List<OrderItem>();
        var productClient = _httpClientFactory.CreateClient("ProductService");
        
        foreach (var itemDto in itemsList)
        {
            var orderItem = new OrderItem
            {
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                Price = itemDto.Price,
                ProductName = itemDto.ProductName,
                ImageUrl = itemDto.ImageUrl,
                CategoryId = itemDto.CategoryId,
                CategoryName = itemDto.CategoryName,
                ProductType = itemDto.ProductType
            };
            
            // Если данные товара не переданы - загружаем из ProductService
            if (string.IsNullOrEmpty(orderItem.ProductName) || orderItem.Price == 0)
            {
                _logger.LogInformation($"Enriching order item for product {itemDto.ProductId}...");
                
                try
                {
                    var productResponse = await productClient.GetAsync($"/api/products/{itemDto.ProductId}");
                    
                    if (productResponse.IsSuccessStatusCode)
                    {
                        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
                        
                        if (product != null)
                        {
                            orderItem.ProductName = product.Name;
                            orderItem.Price = orderItem.Price > 0 ? orderItem.Price : product.Price;
                            orderItem.ImageUrl = product.MediaFiles?.FirstOrDefault()?.Url;
                            orderItem.CategoryId = product.CategoryId;
                            
                            // Загружаем категорию
                            if (product.CategoryId > 0)
                            {
                                try
                                {
                                    var categoryResponse = await productClient.GetAsync($"/api/categories/{product.CategoryId}");
                                    if (categoryResponse.IsSuccessStatusCode)
                                    {
                                        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
                                        orderItem.CategoryName = category?.Name;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Failed to load category {product.CategoryId}");
                                }
                            }
                            
                            _logger.LogInformation($"Product enriched: {orderItem.ProductName}, Price={orderItem.Price}, CategoryId={orderItem.CategoryId}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to load product {itemDto.ProductId}: {productResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading product {itemDto.ProductId}");
                }
            }
            
            // Если атрибуты переданы, сохраняем их как JSON
            if (itemDto.AttributeValues != null && itemDto.AttributeValues.Any())
            {
                var attributesDict = itemDto.AttributeValues.ToDictionary(
                    av => av.AttributeId.ToString(), 
                    av => av.Value
                );
                orderItem.ProductAttributesJson = System.Text.Json.JsonSerializer.Serialize(attributesDict);
            }
            
            enrichedItems.Add(orderItem);
        }
        
        var order = new Order
        {
            CustomerId = orderDto.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.PENDING,
            TotalAmount = enrichedItems.Sum(i => i.Quantity * i.Price),
            UpdatedDate = DateTime.UtcNow,
            OrderItems = enrichedItems
        };

        // =============================================================================
        // Обработка адреса доставки
        // =============================================================================
        ShippingAddressDTO? shippingAddress = customShippingAddress;
        
        if (shippingAddress == null && shippingAddressId.HasValue)
        {
            // Получаем адрес из ShippingAddressService
            try
            {
                var shippingClient = _httpClientFactory.CreateClient("ShippingAddressService");
                var addressResponse = await shippingClient.GetAsync($"/api/shipping-addresses/{shippingAddressId.Value}");
                
                if (addressResponse.IsSuccessStatusCode)
                {
                    shippingAddress = await addressResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to load shipping address {shippingAddressId.Value}");
            }
        }
        else if (shippingAddress == null)
        {
            // Пытаемся получить дефолтный адрес пользователя
            try
            {
                var shippingClient = _httpClientFactory.CreateClient("ShippingAddressService");
                var addressResponse = await shippingClient.GetAsync($"/api/shipping-addresses/customer/{orderDto.CustomerId}/default");
                
                if (addressResponse.IsSuccessStatusCode)
                {
                    shippingAddress = await addressResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to load default shipping address for customer {orderDto.CustomerId}");
            }
        }

        // Сохраняем данные адреса доставки в заказ
        if (shippingAddress != null)
        {
            order.ShippingAddressId = shippingAddress.Id;
            order.ShippingAddress = shippingAddress.Address;
            order.ShippingCity = shippingAddress.City;
            order.ShippingPostalCode = shippingAddress.PostalCode;
            order.ShippingCountry = shippingAddress.Country;
            order.ShippingRecipientName = shippingAddress.RecipientName;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // =============================================================================
        // Уменьшаем количество товаров на складе (только для физических товаров)
        // =============================================================================
        try
        {
            foreach (var item in enrichedItems)
            {
                // Для цифровых товаров не изменяем количество
                if (item.ProductType == ProductType.Digital)
                {
                    _logger.LogInformation($"Digital product {item.ProductId} - skipping quantity reduction");
                    continue;
                }

                // Уменьшаем количество физического товара
                try
                {
                    var updateResponse = await productClient.PutAsJsonAsync(
                        $"/api/products/{item.ProductId}/reduce-quantity", 
                        new { quantity = item.Quantity }
                    );

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Reduced quantity for product {item.ProductId} by {item.Quantity}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to reduce quantity for product {item.ProductId}: {updateResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reducing quantity for product {item.ProductId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product quantities after order creation");
        }

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
        // Переводим заказ в статус CANCELED
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            await UpdateOrderStatus(orderId, OrderStatus.CANCELED);
            
            // Дополнительно очищаем корзину покупателя
            try
            {
                var cartClient = _httpClientFactory.CreateClient("CartService");
                await cartClient.DeleteAsync($"/api/carts/{order.CustomerId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to clear cart for customer {order.CustomerId} after canceling order {orderId}");
            }
        }
    }

    public async Task AddOrderItems(long orderId, OrderItemDTO itemDto)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order != null && order.OrderItems != null)
        {
            var item = new OrderItem
            {
                OrderId = orderId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                Price = itemDto.Price,
                ProductName = itemDto.ProductName,
                ImageUrl = itemDto.ImageUrl,
                CategoryId = itemDto.CategoryId,
                CategoryName = itemDto.CategoryName,
                ProductType = itemDto.ProductType
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

    public async Task<OrderDTO?> CheckoutFromCart(long customerId, long? shippingAddressId = null, ShippingAddressDTO? customShippingAddress = null)
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
            // Если данные уже есть в корзине, используем их (актуальная цена всё равно берётся из ProductService)
            ProductDto? product = null;
            CategoryDto? category = null;
            
            try
            {
                var productResponse = await productClient.GetAsync($"/api/products/{cartItem.ProductId}");
                if (!productResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Товар с ID {cartItem.ProductId} не найден");
                }

                product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
                if (product == null)
                {
                    throw new Exception($"Товар с ID {cartItem.ProductId} не найден");
                }
                
                // Загружаем категорию если её нет в корзине
                if (string.IsNullOrEmpty(cartItem.CategoryName))
                {
                    try
                    {
                        var categoryResponse = await productClient.GetAsync($"/api/categories/{product.CategoryId}");
                        if (categoryResponse.IsSuccessStatusCode)
                        {
                            category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error loading category {product.CategoryId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load product {cartItem.ProductId}");
                throw;
            }

            // =============================================================================
            // НОВАЯ СИСТЕМА: Сохраняем минимально необходимую информацию о товаре
            // =============================================================================
            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Price = product.Price, // Актуальная цена на момент заказа
                
                // Сохраняем информацию о товаре на момент покупки
                ProductName = cartItem.ProductName ?? product.Name,
                ImageUrl = cartItem.ImageUrl ?? product.MediaFiles?.FirstOrDefault()?.Url,
                
                // Категория на момент покупки
                CategoryId = cartItem.CategoryId ?? product.CategoryId,
                CategoryName = cartItem.CategoryName ?? category?.Name,
                
                // Тип товара на момент покупки
                ProductType = product.Type,
                
                // Атрибуты товара на момент покупки (например: RAM=8GB, Color=Black)
                ProductAttributesJson = cartItem.AttributeValues != null && cartItem.AttributeValues.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(
                        cartItem.AttributeValues.ToDictionary(
                            av => av.AttributeId.ToString(),
                            av => av.Value
                        ))
                    : null
            };

            orderItems.Add(orderItem);
            totalAmount += orderItem.Quantity * orderItem.Price;
            
            // =============================================================================
            // Уменьшаем количество товара на складе (только для физических товаров)
            // =============================================================================
            if (product.Type == ProductType.Physical)
            {
                try
                {
                    // Проверяем, достаточно ли товара на складе
                    if (product.Quantity < cartItem.Quantity)
                    {
                        throw new Exception($"Недостаточно товара '{product.Name}' на складе. Доступно: {product.Quantity}, требуется: {cartItem.Quantity}");
                    }
                    
                    // Уменьшаем количество товара
                    var updateRequest = new { quantity = product.Quantity - cartItem.Quantity };
                    var updateResponse = await productClient.PutAsJsonAsync($"/api/products/{product.Id}/quantity", updateRequest);
                    
                    if (!updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Failed to update quantity for product {product.Id}: {updateResponse.StatusCode}");
                    }
                    else
                    {
                        _logger.LogInformation($"Reduced quantity for physical product {product.Id} by {cartItem.Quantity}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating product quantity for {product.Id}");
                    // Не прерываем создание заказа, но логируем ошибку
                }
            }
            else
            {
                _logger.LogInformation($"Digital product {product.Id}, skipping quantity reduction");
            }
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

        // =============================================================================
        // Обработка адреса доставки
        // =============================================================================
        ShippingAddressDTO? shippingAddress = customShippingAddress;
        
        if (shippingAddress == null && shippingAddressId.HasValue)
        {
            // Получаем адрес из ShippingAddressService
            try
            {
                var shippingClient = _httpClientFactory.CreateClient("ShippingAddressService");
                var addressResponse = await shippingClient.GetAsync($"/api/shipping-addresses/{shippingAddressId.Value}");
                
                if (addressResponse.IsSuccessStatusCode)
                {
                    shippingAddress = await addressResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to load shipping address {shippingAddressId.Value}");
            }
        }
        else if (shippingAddress == null)
        {
            // Пытаемся получить дефолтный адрес пользователя
            try
            {
                var shippingClient = _httpClientFactory.CreateClient("ShippingAddressService");
                var addressResponse = await shippingClient.GetAsync($"/api/shipping-addresses/customer/{customerId}/default");
                
                if (addressResponse.IsSuccessStatusCode)
                {
                    shippingAddress = await addressResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to load default shipping address for customer {customerId}");
            }
        }

        // Сохраняем данные адреса доставки в заказ
        if (shippingAddress != null)
        {
            order.ShippingAddressId = shippingAddress.Id;
            order.ShippingAddress = shippingAddress.Address;
            order.ShippingCity = shippingAddress.City;
            order.ShippingPostalCode = shippingAddress.PostalCode;
            order.ShippingCountry = shippingAddress.Country;
            order.ShippingRecipientName = shippingAddress.RecipientName;
        }

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

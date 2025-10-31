using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoMapper;
using CartService.Data;
using CartService.Entities;
using Loft.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CartService.Services;

public class CartService : ICartService
{
    private readonly CartDbContext _db;
    private readonly IMapper _mapper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CartService> _logger;

    public CartService(CartDbContext db, IMapper mapper, IHttpClientFactory httpClientFactory, ILogger<CartService> logger)
    {
        _db = db;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<CartDTO>> GetAllCarts()
    {
        var carts = await _db.Carts.Include(c => c.CartItems).AsNoTracking().ToListAsync();
        var dtos = _mapper.Map<IEnumerable<CartDTO>>(carts).ToList();
        
        foreach (var cart in dtos)
        {
            var needEnrich = cart.CartItems.Any(i => string.IsNullOrEmpty(i.ProductName) || i.Price == 0 || i.CategoryId == null || string.IsNullOrEmpty(i.CategoryName));
            if (needEnrich)
            {
                var enriched = await EnrichCartItemsWithProductInfoReturn(cart.CartItems.ToList());
                cart.CartItems.Clear();
                foreach (var ei in enriched) cart.CartItems.Add(ei);
            }
        }
        
        return dtos;
    }

    public async Task<CartDTO?> GetCartByCustomerId(long customerId)
    {
        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart == null)
        {
            cart = new Cart
            {
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }
        var dto = _mapper.Map<CartDTO>(cart);
        
        var needEnrich = dto.CartItems.Any(i => string.IsNullOrEmpty(i.ProductName) || i.Price == 0 || i.CategoryId == null || string.IsNullOrEmpty(i.CategoryName));
        if (needEnrich)
        {
            var enriched = await EnrichCartItemsWithProductInfoReturn(dto.CartItems.ToList());
            dto.CartItems.Clear();
            foreach (var ei in enriched) dto.CartItems.Add(ei);
        }
        
        return dto;
    }

    public async Task<IEnumerable<CartItemDTO>> GetCartItems(long cartId)
    {
        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart == null) return Enumerable.Empty<CartItemDTO>();
        var items = _mapper.Map<IEnumerable<CartItemDTO>>(cart.CartItems).ToList();
        
        var needEnrich = items.Any(i => string.IsNullOrEmpty(i.ProductName) || i.Price == 0 || i.CategoryId == null || string.IsNullOrEmpty(i.CategoryName));
        if (needEnrich)
        {
            items = await EnrichCartItemsWithProductInfoReturn(items);
        }
        
        return items;
    }

    public async Task<CartDTO> AddToCart(long customerId, long productId, int quantity)
    {
        if (quantity <= 0) quantity = 1;
        
        // Получаем информацию о товаре из ProductService
        ProductInfo? productInfo = null;
        try
        {
            var client = _httpClientFactory.CreateClient();
            var productResponse = await client.GetAsync($"http://productservice:8080/api/products/{productId}");
            
            if (productResponse.IsSuccessStatusCode)
            {
                productInfo = await productResponse.Content.ReadFromJsonAsync<ProductInfo>();
            }
            else
            {
                _logger.LogWarning($"Failed to load product {productId}: {productResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error loading product {productId}");
        }
        
        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart == null)
        {
            cart = new Cart
            {
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Carts.Add(cart);
        }

        var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity += quantity;
            
            // Обновляем информацию о товаре, если она была загружена
            if (productInfo != null)
            {
                existing.ProductName = productInfo.Name;
                existing.Price = productInfo.Price;
                existing.ProductDescription = productInfo.Description;
                // Категорию не сохраняем в БД, отдадим через обогащение DTO
            }
            
            _db.CartItems.Update(existing);
        }
        else
        {
            var item = new CartItem 
            { 
                ProductId = productId, 
                Quantity = quantity, 
                Cart = cart,
                ProductName = productInfo?.Name,
                Price = productInfo?.Price ?? 0,
                ProductDescription = productInfo?.Description
            };
            cart.CartItems.Add(item);
            _db.CartItems.Add(item);
        }

        await _db.SaveChangesAsync();
        
        // Перезагружаем корзину с обновленными данными
        cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        var dto = _mapper.Map<CartDTO>(cart!);
        
        var needEnrich = dto.CartItems.Any(i => string.IsNullOrEmpty(i.ProductName) || i.Price == 0 || i.CategoryId == null || string.IsNullOrEmpty(i.CategoryName));
        if (needEnrich)
        {
            var enriched = await EnrichCartItemsWithProductInfoReturn(dto.CartItems.ToList());
            dto.CartItems.Clear();
            foreach (var ei in enriched) dto.CartItems.Add(ei);
        }
        
        return dto;
    }

    public async Task<CartItemDTO?> UpdateCartItem(long customerId, long productId, int quantity)
    {
        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart == null) return null;
        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item == null) return null;
        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return null;
        }
        item.Quantity = quantity;
        _db.CartItems.Update(item);
        await _db.SaveChangesAsync();
        return _mapper.Map<CartItemDTO>(item);
    }

    public async Task RemoveFormCart(long customerId, long productId)
    {
        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart == null) return;
        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item == null) return;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task ClearCart(long customerId)
    {
        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (cart == null) return;
        _db.CartItems.RemoveRange(cart.CartItems);
        await _db.SaveChangesAsync();
    }

    public async Task MergeCarts(long fromCustomerId, long toCustomerId)
    {
        var fromCart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == fromCustomerId);
        if (fromCart == null) return;
        var toCart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == toCustomerId);
        if (toCart == null)
        {
            toCart = new Cart { CustomerId = toCustomerId };
            _db.Carts.Add(toCart);
            await _db.SaveChangesAsync();
        }

        foreach (var item in fromCart.CartItems.ToList())
        {
            var existing = toCart.CartItems.FirstOrDefault(ci => ci.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
                
                // Обновляем информацию о товаре из исходного элемента
                if (!string.IsNullOrEmpty(item.ProductName))
                {
                    existing.ProductName = item.ProductName;
                    existing.Price = item.Price;
                    existing.ProductDescription = item.ProductDescription;
                }
                
                _db.CartItems.Update(existing);
            }
            else
            {
                var newItem = new CartItem 
                { 
                    ProductId = item.ProductId, 
                    Quantity = item.Quantity, 
                    Cart = toCart,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    ProductDescription = item.ProductDescription
                };
                toCart.CartItems.Add(newItem);
                _db.CartItems.Add(newItem);
            }
        }

        // вместо очистки позиций — удаляем исходную корзину целиком
        _db.Carts.Remove(fromCart);
        await _db.SaveChangesAsync();
    }

    // Новый метод: обогащение информации о товарах из ProductService
    private async Task EnrichCartItemsWithProductInfo(CartDTO cart)
    {
        if (cart.CartItems == null || !cart.CartItems.Any()) return;
        await EnrichCartItemsWithProductInfo(cart.CartItems.ToList());
    }

    private async Task EnrichCartItemsWithProductInfo(List<CartItemDTO> items)
    {
        if (items == null || !items.Any()) return;

        try
        {
            var client = _httpClientFactory.CreateClient();
            
            var enrichedItems = new List<CartItemDTO>();
            
            foreach (var item in items)
            {
                try
                {
                    // Запрашиваем информацию о товаре из ProductService
                    var productResponse = await client.GetAsync($"http://productservice:8080/api/products/{item.ProductId}");
                    
                    if (productResponse.IsSuccessStatusCode)
                    {
                        var product = await productResponse.Content.ReadFromJsonAsync<ProductInfo>();
                        
                        if (product != null)
                        {
                            // Создаем новый CartItemDTO с обогащенными данными (в т.ч. категории)
                            var enrichedItem = item with
                            {
                                Price = product.Price,
                                ProductName = product.Name,
                                ProductDescription = product.Description,
                                CategoryId = product.CategoryId,
                                // Конвертируем CategoryInfo в CategoryDto
                                Category = product.Category != null ? new CategoryDto
                                {
                                    Id = product.Category.Id,
                                    Name = product.Category.Name,
                                    ParentCategoryId = product.Category.ParentCategoryId
                                } : null,
                                CategoryName = product.CategoryName
                            };
                            
                            enrichedItems.Add(enrichedItem);
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to load product {item.ProductId}: {productResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error loading product {item.ProductId}");
                }
                
                // Если не удалось загрузить - оставляем как есть
                enrichedItems.Add(item);
            }
            
            // Обновляем список (это работает для ссылочного типа)
            items.Clear();
            items.AddRange(enrichedItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching cart items with product info");
        }
    }

    private async Task<List<CartItemDTO>> EnrichCartItemsWithProductInfoReturn(List<CartItemDTO> items)
    {
        if (items == null || !items.Any()) return items ?? new List<CartItemDTO>();

        var result = new List<CartItemDTO>(items.Count);
        try
        {
            var client = _httpClientFactory.CreateClient();
            foreach (var item in items)
            {
                try
                {
                    var productResponse = await client.GetAsync($"http://productservice:8080/api/products/{item.ProductId}");
                    if (productResponse.IsSuccessStatusCode)
                    {
                        var product = await productResponse.Content.ReadFromJsonAsync<ProductInfo>();
                        if (product != null)
                        {
                            result.Add(item with
                            {
                                Price = product.Price,
                                ProductName = product.Name,
                                ProductDescription = product.Description,
                                CategoryId = product.CategoryId,
                                // Конвертируем CategoryInfo в CategoryDto
                                Category = product.Category != null ? new CategoryDto
                                {
                                    Id = product.Category.Id,
                                    Name = product.Category.Name,
                                    ParentCategoryId = product.Category.ParentCategoryId
                                } : null,
                                CategoryName = product.CategoryName
                            });
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to load product {item.ProductId}: {productResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error loading product {item.ProductId}");
                }
                // fallback
                result.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching cart items with product info");
            return items; // fallback без изменений
        }
        return result;
    }

    // Вспомогательный класс для десериализации ответа ProductService
    private class ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        
        // =============================================================================
        // НОВАЯ СИСТЕМА: Категория теперь объект, а не просто строка
        // =============================================================================
        public CategoryInfo? Category { get; set; }
        
        // Для обратной совместимости (если Category null)
        public string? CategoryName => Category?.Name;
        
        // =============================================================================
        // НОВАЯ СИСТЕМА: Динамические атрибуты товара
        // =============================================================================
        public List<ProductAttributeInfo>? AttributeValues { get; set; }
    }
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Информация о категории товара
    // =============================================================================
    private class CategoryInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
    }
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Информация об атрибуте товара (например: RAM=8GB, Color=Red)
    // =============================================================================
    private class ProductAttributeInfo
    {
        public int AttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
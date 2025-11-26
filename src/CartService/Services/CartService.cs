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
        ProductDto? productInfo = null;
        CategoryDto? categoryInfo = null;
        try
        {
            var client = _httpClientFactory.CreateClient("ProductService");
            _logger.LogInformation($"Requesting product {productId} from ProductService with base address: {client.BaseAddress}");
            
            var productResponse = await client.GetAsync($"/api/products/{productId}");
            
            _logger.LogInformation($"ProductService response status: {productResponse.StatusCode}");
            
            if (productResponse.IsSuccessStatusCode)
            {
                productInfo = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
                _logger.LogInformation($"Product loaded: Name={productInfo?.Name}, Price={productInfo?.Price}, CategoryId={productInfo?.CategoryId}");
                
                // Получаем информацию о категории
                if (productInfo != null && productInfo.CategoryId > 0)
                {
                    try
                    {
                        var categoryResponse = await client.GetAsync($"/api/categories/{productInfo.CategoryId}");
                        if (categoryResponse.IsSuccessStatusCode)
                        {
                            categoryInfo = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
                            _logger.LogInformation($"Category loaded: Name={categoryInfo?.Name}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to load category {productInfo.CategoryId}: {categoryResponse.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error loading category {productInfo.CategoryId}");
                    }
                }
            }
            else
            {
                var responseBody = await productResponse.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to load product {productId}: {productResponse.StatusCode}, Response: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading product {productId} from ProductService");
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
                existing.ImageUrl = productInfo.MediaFiles?.FirstOrDefault()?.Url;
                existing.CategoryId = productInfo.CategoryId;
                existing.CategoryName = categoryInfo?.Name;
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
                ImageUrl = productInfo?.MediaFiles?.FirstOrDefault()?.Url,
                CategoryId = productInfo?.CategoryId,
                CategoryName = categoryInfo?.Name,
                AddedAt = DateTime.UtcNow
            };
            
            _logger.LogInformation($"Creating new CartItem: ProductId={item.ProductId}, ProductName={item.ProductName}, Price={item.Price}, CategoryId={item.CategoryId}");
            
            cart.CartItems.Add(item);
            _db.CartItems.Add(item);
        }

        _logger.LogInformation($"Saving changes to database...");
        await _db.SaveChangesAsync();
        _logger.LogInformation($"Changes saved successfully");
        
        // Перезагружаем корзину с обновленными данными
        cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == customerId);
        _logger.LogInformation($"Reloaded cart from DB with {cart?.CartItems.Count ?? 0} items");
        
        if (cart != null && cart.CartItems.Any())
        {
            var lastItem = cart.CartItems.OrderByDescending(i => i.AddedAt).First();
            _logger.LogInformation($"Last item from DB: ProductId={lastItem.ProductId}, ProductName={lastItem.ProductName}, Price={lastItem.Price}, CategoryId={lastItem.CategoryId}");
        }
        
        var dto = _mapper.Map<CartDTO>(cart!);
        
        _logger.LogInformation($"Mapped to DTO with {dto.CartItems.Count} items");
        if (dto.CartItems.Any())
        {
            var lastDtoItem = dto.CartItems.OrderByDescending(i => i.AddedAt).First();
            _logger.LogInformation($"Last DTO item: ProductId={lastDtoItem.ProductId}, ProductName={lastDtoItem.ProductName}, Price={lastDtoItem.Price}, CategoryId={lastDtoItem.CategoryId}");
        }
        
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
                    existing.ImageUrl = item.ImageUrl;
                    existing.CategoryId = item.CategoryId;
                    existing.CategoryName = item.CategoryName;
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
                    ImageUrl = item.ImageUrl,
                    CategoryId = item.CategoryId,
                    CategoryName = item.CategoryName,
                    AddedAt = item.AddedAt
                };
                toCart.CartItems.Add(newItem);
                _db.CartItems.Add(newItem);
            }
        }

        // вместо очистки позиций — удаляем исходную корзину целиком
        _db.Carts.Remove(fromCart);
        await _db.SaveChangesAsync();
    }


    private async Task<List<CartItemDTO>> EnrichCartItemsWithProductInfoReturn(List<CartItemDTO> items)
    {
        if (items == null || !items.Any()) return items ?? new List<CartItemDTO>();

        var result = new List<CartItemDTO>(items.Count);
        try
        {
            var client = _httpClientFactory.CreateClient("ProductService");
            foreach (var item in items)
            {
                // Если данные уже есть, используем их
                if (!string.IsNullOrEmpty(item.ProductName) && item.Price > 0 && item.CategoryId != null)
                {
                    result.Add(item);
                    continue;
                }
                
                // Если данных нет - загружаем из ProductService
                try
                {
                    var productResponse = await client.GetAsync($"/api/products/{item.ProductId}");
                    if (productResponse.IsSuccessStatusCode)
                    {
                        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
                        if (product != null)
                        {
                            // Пытаемся получить категорию
                            CategoryDto? category = null;
                            try
                            {
                                var categoryResponse = await client.GetAsync($"/api/categories/{product.CategoryId}");
                                if (categoryResponse.IsSuccessStatusCode)
                                {
                                    category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Error loading category {product.CategoryId}");
                            }
                            
                            result.Add(item with
                            {
                                Price = product.Price,
                                ProductName = product.Name,
                                ImageUrl = product.MediaFiles?.FirstOrDefault()?.Url,
                                CategoryId = product.CategoryId,
                                CategoryName = category?.Name,
                                Category = null
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
}
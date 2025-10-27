using System.Collections.Generic;
using System.Threading.Tasks;
using Loft.Common.DTOs;

namespace CartService.Services;

public interface ICartService
{
    Task<IEnumerable<CartDTO>> GetAllCarts();
    Task<CartDTO?> GetCartByCustomerId(long customerId);
    Task<IEnumerable<CartItemDTO>> GetCartItems(long cartId);
    Task<CartDTO> AddToCart(long customerId,long productId,int quantity);
    Task<CartItemDTO?> UpdateCartItem(long customerId,long productId,int quantity);
    Task RemoveFormCart(long customerId,long productId);
    Task ClearCart(long customerId);
    Task MergeCarts(long fromCustomerId,long toCustomerId);
    
    /*
     * Примечания: GetCartByCustomerId может возвращать null,
     * если корзины нет; Update возвращает null если элемент отсутствует.
     */
}
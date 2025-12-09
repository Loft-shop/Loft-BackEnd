using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CartService.Services;
using Loft.Common.DTOs;

namespace CartService.Controllers
{
    [ApiController]
    [Route("api/carts")]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartsController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // GET api/carts
        [HttpGet("")]
        public async Task<IActionResult> GetAllCarts()
        {
            var carts = await _cartService.GetAllCarts();
            return Ok(carts);
        }

        // GET api/carts/{customerId} - Альтернативный маршрут для получения корзины
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCart(long customerId)
        {
            var cart = await _cartService.GetCartByCustomerId(customerId);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        // GET api/carts/customer/{customerId}
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCartByCustomer(long customerId)
        {
            var cart = await _cartService.GetCartByCustomerId(customerId);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        // GET api/carts/{cartId}/items
        [HttpGet("{cartId}/items")]
        public async Task<IActionResult> GetCartItems(long cartId)
        {
            var items = await _cartService.GetCartItems(cartId);
            return Ok(items);
        }

        // POST api/carts/{customerId}/items
        [HttpPost("{customerId}/items")]
        public async Task<IActionResult> AddToCart(long customerId, [FromBody] AddItemRequest req)
        {
            if (req == null) return BadRequest();
            var cart = await _cartService.AddToCart(customerId, req.ProductId, req.Quantity);
            return Ok(cart);
        }

        // PUT api/carts/{customerId}/items
        [HttpPut("{customerId}/items")]
        public async Task<IActionResult> UpdateCartItem(long customerId, [FromBody] UpdateItemRequest req)
        {
            if (req == null) return BadRequest();
            
            try
            {
                var item = await _cartService.UpdateCartItem(customerId, req.ProductId, req.Quantity);
                if (item == null) return NotFound();
                return Ok(item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE api/carts/{customerId}/items/{productId}
        [HttpDelete("{customerId}/items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(long customerId, long productId)
        {
            await _cartService.RemoveFormCart(customerId, productId);
            return NoContent();
        }

        // DELETE api/carts/{customerId}
        [HttpDelete("{customerId}")]
        public async Task<IActionResult> ClearCart(long customerId)
        {
            await _cartService.ClearCart(customerId);
            return NoContent();
        }

        // POST api/carts/merge
        [HttpPost("merge")]
        public async Task<IActionResult> MergeCarts([FromBody] MergeRequest req)
        {
            if (req == null) return BadRequest();
            await _cartService.MergeCarts(req.FromCustomerId, req.ToCustomerId);
            return NoContent();
        }

        // Вспомогательные типы запросов
        public record AddItemRequest(long ProductId, int Quantity);
        public record UpdateItemRequest(long ProductId, int Quantity);
        public record MergeRequest(long FromCustomerId, long ToCustomerId);
    }
}
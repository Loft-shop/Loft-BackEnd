using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var orderDto = new OrderDTO(
                Id: 0, 
                CustomerId: request.Order.CustomerId, 
                OrderDate: DateTime.UtcNow, 
                Status: OrderStatus.PENDING, 
                TotalAmount: 0
            );
            var order = await _orderService.CreateOrder(orderDto, request.Items);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetOrderById(long id)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        [HttpGet("customer/{customerId:long}")]
        public async Task<IActionResult> GetOrdersByCustomerId(long customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var orders = await _orderService.GetOrdersByCustomerId(customerId, page, pageSize);
            return Ok(orders);
        }

        [HttpPut("{id:long}/status")]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] OrderStatus status)
        {
            await _orderService.UpdateOrderStatus(id, status);
            return NoContent();
        }

        [HttpPut("{id:long}/cancel")]
        public async Task<IActionResult> CancelOrder(long id)
        {
            await _orderService.CancelOrder(id);
            return NoContent();
        }

        [HttpPost("{id:long}/items")]
        public async Task<IActionResult> AddOrderItems(long id, [FromBody] OrderItemDTO item)
        {
            await _orderService.AddOrderItems(id, item);
            return NoContent();
        }

        [HttpDelete("{id:long}/items/{itemId:long}")]
        public async Task<IActionResult> RemoveOrderItems(long id, long itemId)
        {
            await _orderService.RemoveOrderItems(id, itemId);
            return NoContent();
        }

        [HttpPost("calculate-total")]
        public async Task<IActionResult> CalculateOrderTotal([FromBody] IEnumerable<OrderItemDTO> items)
        {
            var total = await _orderService.CalculateOrderTotal(items);
            return Ok(new { Total = total });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrders();
            return Ok(orders);
        }

        // POST api/orders/checkout/{customerId}
        [HttpPost("checkout/{customerId:long}")]
        public async Task<IActionResult> CheckoutFromCart(long customerId)
        {
            try
            {
                var order = await _orderService.CheckoutFromCart(customerId);
                if (order == null)
                {
                    return BadRequest("Не удалось создать заказ");
                }
                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class CreateOrderRequest
    {
        public required CreateOrderInput Order { get; set; }
        public required IEnumerable<OrderItemDTO> Items { get; set; }
    }

    public class CreateOrderInput
    {
        public long CustomerId { get; set; }
    }
}
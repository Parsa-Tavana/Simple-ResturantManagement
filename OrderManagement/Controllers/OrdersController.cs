using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.DTOs;
using OrderManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;
        public OrdersController(IOrderService service) => _service = service;

        [HttpPost]
        [Authorize(Roles = "Customer,RestaurantAdmin,SuperAdmin")]

        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
        {
            var created = await _service.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDto>> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            await _service.UpdateStatusAsync(id, dto.Status);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
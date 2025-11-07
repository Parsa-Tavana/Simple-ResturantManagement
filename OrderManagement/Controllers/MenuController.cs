using Microsoft.AspNetCore.Mvc;
using OrderManagement.DTOs;
using OrderManagement.Repositories;

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/restaurants/{restaurantId:int}/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuItemRepository _menuRepo;
        public MenuController(IMenuItemRepository menuRepo) => _menuRepo = menuRepo;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenu(int restaurantId)
        {
            var items = await _menuRepo.GetByRestaurantAsync(restaurantId);
            var dtos = items.Select(m => new MenuItemDto
            {
                Id = m.Id,
                RestaurantId = m.RestaurantId,
                Name = m.Name,
                Price = m.Price,
                IsAvailable = m.IsAvailable
            });
            return Ok(dtos);
        }
    }
}

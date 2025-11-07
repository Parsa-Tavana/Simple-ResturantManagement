using System.Collections.Generic;

namespace OrderManagement.DTOs
{
    public class CreateOrderItemDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public int RestaurantId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}

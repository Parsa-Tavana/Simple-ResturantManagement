using System;

namespace OrderManagement.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int RestaurantId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RestaurantName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    //public class CreateOrderDto
    //{
    //    public int CustomerId { get; set; }
    //    public int RestaurantId { get; set; }
    //    public decimal TotalPrice { get; set; }
    //}

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
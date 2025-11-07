namespace OrderManagement.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public int MenuItemId { get; set; }

        // Snapshot fields: keep historical accuracy even if menu changes
        public string NameSnapshot { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}

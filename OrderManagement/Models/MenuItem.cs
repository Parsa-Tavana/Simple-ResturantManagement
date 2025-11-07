namespace OrderManagement.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; } = default!;

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}

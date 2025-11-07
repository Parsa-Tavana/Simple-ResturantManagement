using OrderManagement.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // NEW
    public DateTime? PaidAt { get; set; }
    public string? StripeSessionId { get; set; }

    public Customer? Customer { get; set; }
    public Restaurant? Restaurant { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

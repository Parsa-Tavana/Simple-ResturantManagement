namespace OrderManagement.DTOs
{
    public class CreateCheckoutDto
    {
            public int OrderId { get; set; }
            public string? SuccessUrl { get; set; }
            public string? CancelUrl { get; set; }
        
    }
}

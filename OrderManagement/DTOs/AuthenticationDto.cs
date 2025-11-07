namespace OrderManagement.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Role { get; set; } // "Customer" | "RestaurantAdmin" | "SuperAdmin"
        public int? CustomerId { get; set; }
        public int? RestaurantId { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}

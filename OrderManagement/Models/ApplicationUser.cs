using Microsoft.AspNetCore.Identity;

namespace OrderManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Optional linkage for convenience:
        public int? CustomerId { get; set; }
        public int? RestaurantId { get; set; }
    }
}

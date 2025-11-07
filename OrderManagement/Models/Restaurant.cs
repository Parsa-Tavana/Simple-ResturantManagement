using System;
using System.Collections.Generic;

namespace OrderManagement.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<MenuItem> MenuItems { get; set; } = new();

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
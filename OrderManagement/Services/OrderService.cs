using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.DTOs;
using OrderManagement.Models;
using OrderManagement.Repositories;

namespace OrderManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly AppDbContext _db;

        public OrderService(IOrderRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            // Validate & load menu items
            var requestedIds = dto.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var menuItems = await _db.MenuItems
                                     .Where(m => requestedIds.Contains(m.Id))
                                     .ToListAsync();

            if (menuItems.Count != requestedIds.Count)
                throw new ArgumentException("One or more menu items were not found.");

            if (menuItems.Any(m => m.RestaurantId != dto.RestaurantId))
                throw new ArgumentException("All items must belong to the selected restaurant.");

            // Build order + snapshot items
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                RestaurantId = dto.RestaurantId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                OrderItems = dto.Items.Select(i =>
                {
                    var m = menuItems.First(mi => mi.Id == i.MenuItemId);
                    return new OrderItem
                    {
                        MenuItemId = m.Id,
                        NameSnapshot = m.Name,
                        UnitPrice = m.Price,
                        Quantity = i.Quantity
                    };
                }).ToList()
            };

            // Server-calculated total
            order.TotalPrice = order.OrderItems.Sum(x => x.UnitPrice * x.Quantity);

            // Persist (through repo so includes stay consistent with your app)
            var saved = await _repo.AddAsync(order);

            // Re-load with navs/items (if AddAsync didn't include them)
            var created = await _repo.GetByIdAsync(saved.Id) ?? saved;
            return ToDto(created);
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync() =>
            (await _repo.GetAllAsync())
                .OrderByDescending(o => o.CreatedAt)
                .Select(ToDto);

        public async Task UpdateStatusAsync(int id, string newStatus)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new KeyNotFoundException("Order not found");
            entity.Status = newStatus;
            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);

        private static OrderDto ToDto(Order o) => new OrderDto
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            RestaurantId = o.RestaurantId,
            CustomerName = o.Customer?.Name ?? string.Empty,
            RestaurantName = o.Restaurant?.Name ?? string.Empty,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems?.Select(oi => new OrderItemDto
            {
                MenuItemId = oi.MenuItemId,
                Name = oi.NameSnapshot,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                LineTotal = oi.UnitPrice * oi.Quantity
            }).ToList() ?? new List<OrderItemDto>()
        };
    }
}

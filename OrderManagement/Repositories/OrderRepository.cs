using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        public OrderRepository(AppDbContext context) => _context = context;

        public async Task<Order> AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<IEnumerable<Order>> GetAllAsync() =>
            await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)     // include items for totals/details
                .AsSplitQuery()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<Order?> GetByIdAsync(int id) =>
            await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Orders.FindAsync(id);
            if (entity != null)
            {
                _context.Orders.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}

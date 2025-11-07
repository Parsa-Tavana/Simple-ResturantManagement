using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public class RestaurantRepository : IRestaurantRepository
    {
        private readonly AppDbContext _context;
        public RestaurantRepository(AppDbContext context) => _context = context;

        public async Task<IEnumerable<Restaurant>> GetAllAsync() =>
            await _context.Restaurants.AsNoTracking().ToListAsync();

        public async Task<Restaurant?> GetByIdAsync(int id) =>
            await _context.Restaurants.FindAsync(id);

        public async Task<Restaurant> AddAsync(Restaurant restaurant)
        {
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }

        public async Task UpdateAsync(Restaurant restaurant)
        {
            _context.Restaurants.Update(restaurant);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Restaurants.FindAsync(id);
            if (entity != null)
            {
                _context.Restaurants.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Restaurants.AnyAsync(r => r.Id == id);
    }
}
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public class MenuItemRepository : IMenuItemRepository
    {
        private readonly AppDbContext _db;
        public MenuItemRepository(AppDbContext db) => _db = db;

        public Task<List<MenuItem>> GetByRestaurantAsync(int restaurantId) =>
            _db.MenuItems.Where(m => m.RestaurantId == restaurantId && m.IsAvailable).OrderBy(m => m.Name).ToListAsync();

        public Task<MenuItem?> GetByIdAsync(int id) => _db.MenuItems.FindAsync(id).AsTask();

        public async Task<MenuItem> AddAsync(MenuItem item)
        {
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(MenuItem item)
        {
            _db.MenuItems.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.MenuItems.FindAsync(id);
            if (entity != null)
            {
                _db.MenuItems.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}

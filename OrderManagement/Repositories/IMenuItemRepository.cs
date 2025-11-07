using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface IMenuItemRepository
    {
        Task<List<MenuItem>> GetByRestaurantAsync(int restaurantId);
        Task<MenuItem?> GetByIdAsync(int id);
        Task<MenuItem> AddAsync(MenuItem item);
        Task UpdateAsync(MenuItem item);
        Task DeleteAsync(int id);
    }
}

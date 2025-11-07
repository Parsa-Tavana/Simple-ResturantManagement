using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface IRestaurantRepository
    {
        Task<IEnumerable<Restaurant>> GetAllAsync();
        Task<Restaurant?> GetByIdAsync(int id);
        Task<Restaurant> AddAsync(Restaurant restaurant);
        Task UpdateAsync(Restaurant restaurant);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagement.DTOs;

namespace OrderManagement.Services
{
    public interface IRestaurantService
    {
        Task<IEnumerable<RestaurantDto>> GetAllAsync();
        Task<RestaurantDto?> GetByIdAsync(int id);
        Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto);
        Task UpdateAsync(int id, CreateRestaurantDto dto);
        Task DeleteAsync(int id);
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderManagement.DTOs;
using OrderManagement.Models;
using OrderManagement.Repositories;

namespace OrderManagement.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly IRestaurantRepository _repo;
        public RestaurantService(IRestaurantRepository repo) => _repo = repo;

        public async Task<IEnumerable<RestaurantDto>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(ToDto);

        public async Task<RestaurantDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
        {
            var entity = await _repo.AddAsync(new Restaurant
            {
                Name = dto.Name,
                Address = dto.Address,
                Phone = dto.Phone
            });
            return ToDto(entity);
        }

        public async Task UpdateAsync(int id, CreateRestaurantDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return;
            entity.Name = dto.Name;
            entity.Address = dto.Address;
            entity.Phone = dto.Phone;
            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);

        private static RestaurantDto ToDto(Restaurant r) => new RestaurantDto
        {
            Id = r.Id,
            Name = r.Name,
            Address = r.Address,
            Phone = r.Phone
        };
    }
}
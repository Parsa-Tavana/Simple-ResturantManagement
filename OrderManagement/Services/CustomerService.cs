using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderManagement.DTOs;
using OrderManagement.Models;
using OrderManagement.Repositories;

namespace OrderManagement.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        public CustomerService(ICustomerRepository repo) => _repo = repo;

        public async Task<IEnumerable<CustomerDto>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(ToDto);

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
        {
            var entity = await _repo.AddAsync(new Customer
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone
            });
            return ToDto(entity);
        }

        public async Task UpdateAsync(int id, CreateCustomerDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return;
            entity.Name = dto.Name;
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);

        private static CustomerDto ToDto(Customer c) => new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone
        };
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagement.DTOs;

namespace OrderManagement.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllAsync();
        Task<CustomerDto?> GetByIdAsync(int id);
        Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
        Task UpdateAsync(int id, CreateCustomerDto dto);
        Task DeleteAsync(int id);
    }
}
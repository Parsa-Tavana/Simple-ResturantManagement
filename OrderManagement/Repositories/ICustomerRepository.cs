using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface ICustomerRepository
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    }
}
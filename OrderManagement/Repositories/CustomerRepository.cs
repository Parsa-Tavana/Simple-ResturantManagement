using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;
        public CustomerRepository(AppDbContext context) => _context = context;

        public async Task<IEnumerable<Customer>> GetAllAsync() =>
            await _context.Customers.AsNoTracking().ToListAsync();

        public async Task<Customer?> GetByIdAsync(int id) =>
            await _context.Customers.FindAsync(id);

        public async Task<Customer> AddAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Customers.FindAsync(id);
            if (entity != null)
            {
                _context.Customers.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Customers.AnyAsync(c => c.Id == id);

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
            await _context.Customers.AnyAsync(c => c.Email == email && (excludeId == null || c.Id != excludeId));
    }
}
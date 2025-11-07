using OrderManagement.Models;


namespace OrderManagement.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetAllAsync();
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
    }
}
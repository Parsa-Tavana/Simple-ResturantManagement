using OrderManagement.DTOs;


namespace OrderManagement.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<OrderDto?> GetByIdAsync(int id);
        Task<IEnumerable<OrderDto>> GetAllAsync();
        Task UpdateStatusAsync(int id, string newStatus);
        Task DeleteAsync(int id);
    }
}
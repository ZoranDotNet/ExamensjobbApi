using exjobb.Entities;

namespace exjobb.Repositories.Interface
{
    public interface IProductsRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> ExistsAsync(int id);
        Task DeleteProductAsync(int id);
        Task<IEnumerable<Product>> GetProductsByName(string? name);
    }
}

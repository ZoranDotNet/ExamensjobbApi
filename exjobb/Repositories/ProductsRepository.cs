using exjobb.Data;
using exjobb.Entities;
using exjobb.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace exjobb.Repositories
{
    public class ProductsRepository(AppDbContext context) : IProductsRepository
    {
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await context.Products.AsNoTracking().ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            var productFromDb = await context.Products.FindAsync(product.Id);
            if (productFromDb is null)
            {
                return false;
            }
            productFromDb.Name = product.Name;
            productFromDb.Color = product.Color;
            productFromDb.ImageUrl = product.ImageUrl;
            productFromDb.Description = product.Description;
            productFromDb.Price = product.Price;

            context.Products.Update(productFromDb);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteProductAsync(int id)
        {
            await context.Products.Where(p => p.Id == id).ExecuteDeleteAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Enumerable.Empty<Product>();
            }
            return await context.Products.Where(p => p.Name.ToLower().Contains(name.ToLower())).ToListAsync();
        }

        public Task<bool> ExistsAsync(int id)
        {
            return context.Products.AnyAsync(p => p.Id == id);
        }
    }
}

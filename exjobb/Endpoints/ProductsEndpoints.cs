using exjobb.Entities;
using exjobb.Models;
using exjobb.Repositories.Interface;
using Microsoft.AspNetCore.OutputCaching;

namespace exjobb.Endpoints
{
    public static class ProductsEndpoints
    {
        public static RouteGroupBuilder MapProducts(this RouteGroupBuilder group)
        {
            group.MapGet("/", GetProducts).CacheOutput(c => c.Expire(TimeSpan.FromSeconds(120)).Tag("products-get"));
            group.MapGet("/{id:int}", GetProduct);
            group.MapPost("/", CreateProduct).RequireAuthorization("Admin");
            group.MapPut("/{id:int}", UpdateProduct).RequireAuthorization("Admin");
            group.MapDelete("/{id:int}", DeleteProduct).RequireAuthorization("Admin");

            return group;
        }


        static async Task<IResult> GetProducts(IProductsRepository productsRepository)
        {
            var products = await productsRepository.GetAllAsync();

            var productsDto = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Color = p.Color,
                ImageUrl = p.ImageUrl,
                Description = p.Description,
                Price = p.Price
            }).ToList();

            return TypedResults.Ok(productsDto);
        }

        static async Task<IResult> GetProduct(IProductsRepository productsRepository, int id)
        {
            var product = await productsRepository.GetByIdAsync(id);
            if (product is null)
            {
                return TypedResults.NotFound($"Product with id {id} could not be found");
            }

            var productDto = product;

            return TypedResults.Ok(productDto);
        }

        static async Task<IResult> CreateProduct(IProductsRepository productsRepository, IOutputCacheStore outputCacheStore, CreateProductDto dto)
        {
            try
            {
                var product = new Product
                {
                    Name = dto.Name,
                    Color = dto.Color,
                    ImageUrl = dto.ImageUrl,
                    Description = dto.Description,
                    Price = dto.Price
                };
                await productsRepository.CreateProductAsync(product);
                await outputCacheStore.EvictByTagAsync("products-get", default);

                return TypedResults.Created($"/api/products/{product.Id}", product);
            }
            catch (Exception e)
            {
                return TypedResults.Problem($"Details: {e.Message}", statusCode: 500);
            }
        }

        static async Task<IResult> UpdateProduct(int id, CreateProductDto dto, IProductsRepository productsRepository, IOutputCacheStore outputCacheStore)
        {
            try
            {
                Product product = new()
                {
                    Id = id,
                    Name = dto.Name,
                    Color = dto.Color,
                    ImageUrl = dto.ImageUrl,
                    Description = dto.Description,
                    Price = dto.Price
                };
                var updatedSuccess = await productsRepository.UpdateProductAsync(product);
                if (!updatedSuccess)
                {
                    return TypedResults.NotFound($"Product with id {id} could not be found");
                }

                await outputCacheStore.EvictByTagAsync("products-get", default);

                return TypedResults.NoContent();
            }
            catch (Exception)
            {
                return TypedResults.Problem($"There was a problem updating the product", statusCode: 500);
            }
        }

        static async Task<IResult> DeleteProduct(IProductsRepository productsRepository, IOutputCacheStore outputCacheStore, int id)
        {
            var productExists = await productsRepository.ExistsAsync(id);
            if (!productExists)
            {
                return TypedResults.NotFound($"Product with id {id} could not be found");
            }
            await productsRepository.DeleteProductAsync(id);
            await outputCacheStore.EvictByTagAsync("products-get", default);
            return TypedResults.NoContent();
        }
    }
}

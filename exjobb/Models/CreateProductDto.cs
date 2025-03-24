namespace exjobb.Models
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
    }
}

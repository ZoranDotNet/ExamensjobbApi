using System.ComponentModel.DataAnnotations;

namespace exjobb.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Color { get; set; } = string.Empty;

        [StringLength(200)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }

    }
}

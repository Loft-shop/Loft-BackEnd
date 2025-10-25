namespace monolith_version.DTOs
{
    public class ProductFilterDto
    {
        public int? CategoryId { get; set; }
        public int? SellerId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<ProductAttributeFilterDto>? AttributeFilters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

using System.Collections.Generic;
using Loft.Common.Enums;

namespace Loft.Common.DTOs
{
    public class CategoryDto
    {
        public int? Id { get; set; }
        public int? ParentCategoryId { get; set; }
        public string Name { get; set; } = null!;

        public string? ImageUrl { get; set; } = null!;
        public ModerationStatus? Status { get; set; }

        public ProductType Type { get; set; }

        public int? ViewCount { get; set; }
        
        public ICollection<CategoryAttributeDto>? Attributes { get; set; }
        
        public ICollection<CategoryDto>? SubCategories { get; set; }
    }

    public class CategoryAttributeDto
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = null!;
        public bool IsRequired { get; set; }
        public int OrderIndex { get; set; }
    }
}
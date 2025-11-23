using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loft.Common.DTOs
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

public class PagedResultFilterDto<T>
{
    public int TotalCount { get; set; }    // общее количество товаров
    public int TotalPages { get; set; }    // общее количество страниц
    public int Page { get; set; }          // текущая страница
    public int PageSize { get; set; }      // размер страницы
    public IEnumerable<T> Items { get; set; }  // список товаров на странице
}
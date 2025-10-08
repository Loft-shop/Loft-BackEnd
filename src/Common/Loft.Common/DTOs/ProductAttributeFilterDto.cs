using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loft.Common.DTOs
{
    public class ProductAttributeFilterDto
    {
        public int AttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}

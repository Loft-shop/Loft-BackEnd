 using AutoMapper;
using Loft.Common.DTOs;
using ProductService.Entities;

namespace ProductService.Mappings
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // ---------------- PRODUCT ----------------
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.AttributeValues, opt => opt.MapFrom(src => src.AttributeValues))
                .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ReverseMap(); // ������������ �������

            // ---------------- PRODUCT ATTRIBUTE VALUE ----------------
            CreateMap<ProductAttributeValue, ProductAttributeValueDto>()
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
            .ReverseMap();

            // ---------------- MEDIA FILE ----------------
            CreateMap<MediaFile, MediaFileDto>().ReverseMap();

            // ---------------- COMMENT ----------------
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles))
                .ReverseMap();

            // ---------------- CATEGORY ----------------
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.CategoryAttributes))
                .ForMember(dest => dest.SubCategories, opt => opt.MapFrom(src => src.SubCategories))
                .ReverseMap();

            // ---------------- CATEGORY ATTRIBUTE ----------------
            CreateMap<CategoryAttribute, CategoryAttributeDto>()
                .ForMember(dest => dest.AttributeName, opt => opt.MapFrom(src => src.Attribute.Name))
                .ReverseMap();

            // ---------------- ATTRIBUTE ENTITY ----------------
            CreateMap<AttributeEntity, AttributeDto>().ReverseMap();
        }
    }
}
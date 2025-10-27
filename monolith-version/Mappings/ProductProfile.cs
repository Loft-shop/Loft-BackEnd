using AutoMapper;
using Microsoft.CodeAnalysis;
using monolith_version.DTOs;
using monolith_version.Models.Entities;

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
                .ReverseMap(); // двусторонний маппинг

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

            //  ---------------- ATTRIBUTE ENTITY ----------------
            CreateMap<CategoryAttribute, CategoryAttributeDto>().ReverseMap();

            // ---------------- ATTRIBUTE ENTITY ----------------
            CreateMap<AttributeEntity, AttributeDto>().ReverseMap();
        }
    }
}
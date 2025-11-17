using AutoMapper;
using Loft.Common.DTOs;
using MediaService.Entities;

namespace MediaService.Mappings
{
    public class MediaProfile : Profile
    {
        public MediaProfile()
        {
            // ---------------- MEDIA FILE ----------------
            CreateMap<MediaFile, MediaFileDTO>()
                .ReverseMap();

            // ---------------- UPLOAD RESPONSE ----------------
            CreateMap<MediaFile, UploadResponseDTO>()
                .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.CreatedAt));

            // Обратный маппинг для создания MediaFile из DTO
            CreateMap<UploadResponseDTO, MediaFile>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.UploadedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UploadedAt));
        }
    }
}


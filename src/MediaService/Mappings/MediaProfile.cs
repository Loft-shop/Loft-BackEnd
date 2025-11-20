using AutoMapper;
using Loft.Common.DTOs;
using MediaService.Entities;

namespace MediaService.Mappings
{
    public class MediaProfile : Profile
    {
        public MediaProfile()
        {
            CreateMap<MediaFile, MyPublicMediaFileDTO>();
        }
    }
}


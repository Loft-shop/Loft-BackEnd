using System.Collections.Generic;
using System.Reflection.Emit;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using MediaService.Entities;

namespace MediaService.Data
{
    public class MediaDbContext : DbContext
    {
        public MediaDbContext(DbContextOptions<MediaDbContext> options)
            : base(options) { }

        public DbSet<MediaFile> MediaFiles { get; set; } = null!;
        public DbSet<DownloadToken> DownloadTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}

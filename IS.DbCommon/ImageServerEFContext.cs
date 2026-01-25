using IS.DbCommon.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IS.DbCommon
{
    public class ImageServerEFContext : DbContext
    {
        public ImageServerEFContext(DbContextOptions<ImageServerEFContext> options)
        : base(options)
        {
        }

        public DbSet<Image> Images => Set<Image>();
        public DbSet<Metadata> Metadata => Set<Metadata>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Person> Persons => Set<Person>();
        public DbSet<Face> Faces => Set<Face>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Image ----------
            modelBuilder.Entity<Image>(entity =>
            {
                entity.ToTable("images");

                entity.HasKey(i => i.Id);

                entity.Property(i => i.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(i => i.RelativePath)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(i => i.PerceptualHash)
                    .HasMaxLength(128);

                entity.Property(i => i.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // если PostgreSQL:
                entity.Property(i => i.Labels)
                    .HasColumnType("text[]");

                entity.HasOne(i => i.Metadata)
                    .WithOne(m => m.Image)
                    .HasForeignKey<Metadata>(m => m.PhotoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.PostedByAccount)
                    .WithMany(a => a.PostedImages)
                    .HasForeignKey(i => i.PostedByAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Metadata ----------
            modelBuilder.Entity<Metadata>(entity =>
            {
                entity.ToTable("photo_metadata");

                entity.HasKey(m => m.PhotoId);

                entity.Property(m => m.MimeType)
                    .HasMaxLength(256);

                entity.Property(m => m.Extension)
                    .HasMaxLength(16);

                entity.Property(m => m.HashSha1)
                    .HasMaxLength(40);

                entity.Property(m => m.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(m => m.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ---------- Account ----------
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("accounts");

                entity.HasKey(a => a.Id);

                entity.Property(a => a.Login)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(a => a.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(a => a.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // PostgreSQL text[]
                entity.Property(a => a.Permissions)
                    .HasColumnType("text[]");
            });

            // ---------- Person ----------
            modelBuilder.Entity<Person>(entity =>
            {
                entity.ToTable("persons");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.DisplayName)
                    .HasMaxLength(256);

                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(p => p.Account)
                    .WithOne(a => a.Person)
                    .HasForeignKey<Person>(p => p.AccountId)
                    .OnDelete(DeleteBehavior.SetNull);

                // основное лицо (аватарка)
                entity.HasOne(p => p.MainFace)
                    .WithOne()
                    .HasForeignKey<Person>(p => p.FaceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- Face ----------
            modelBuilder.Entity<Face>(entity =>
            {
                entity.ToTable("faces");

                entity.HasKey(f => f.Id);

                entity.Property(f => f.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(f => f.Confidence);

                // если PostgreSQL, можно явно указать тип:
                // entity.Property(f => f.Embedding).HasColumnType("bytea");

                entity.HasOne(f => f.Photo)
                    .WithMany(i => i.Faces)
                    .HasForeignKey(f => f.PhotoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Person)
                    .WithMany(p => p.Faces)
                    .HasForeignKey(f => f.PersonId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

        }
    }
}

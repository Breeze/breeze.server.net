using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace ProduceTPH
{
    public partial class ProduceTPHContext : DbContext
    {
        public ProduceTPHContext()
        {
        }

        public ProduceTPHContext(DbContextOptions<ProduceTPHContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ItemOfProduce> ItemsOfProduce { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=ProduceTPH;Integrated Security=True;MultipleActiveResultSets=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<ItemOfProduce>(entity =>
            {
                entity.ToTable("ItemOfProduce");

                entity.HasDiscriminator<string>("ItemType")
                        .HasValue<Fruit>("Fruit")
                        .HasValue<Vegetable>("Vegetable")
#if NET5_0_OR_GREATER
                        .IsComplete(false)
#endif
                ;
#if NETSTANDARD
                entity.HasDiscriminator<string>("ItemSubtype")
                        .HasValue<Apple>("Apple")
                        .HasValue<Strawberry>("Strawberry")
                        .HasValue<Tomato>("Tomato")
                        .HasValue<WhitePotato>("WhitePotato");
#endif

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ItemNumber)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsFixedLength(true);

                //entity.Property(e => e.ItemSubtype).HasMaxLength(50);
                //entity.Property(e => e.ItemType).HasMaxLength(20);

                entity.Property(e => e.QuantityPerUnit).HasMaxLength(20);

                entity.Property(e => e.UnitPrice).HasColumnType("money");
            });

            modelBuilder.Entity<Fruit>(entity => {
#if NET5_0_OR_GREATER
                entity.HasDiscriminator<string>("ItemSubtype")
                        .HasValue<Apple>("Apple")
                        .HasValue<Strawberry>("Strawberry")
                        .IsComplete(false);
#endif
                entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("Name");
                entity.Property(e => e.USDACategory).HasMaxLength(50).HasColumnName("USDACategory");
            });

            modelBuilder.Entity<Vegetable>(entity => {
#if NET5_0_OR_GREATER
                entity.HasDiscriminator<string>("ItemSubtype")
                        .HasValue<Tomato>("Tomato")
                        .HasValue<WhitePotato>("WhitePotato")
                        .IsComplete(false);
#endif
                entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("Name");
                entity.Property(e => e.USDACategory).HasMaxLength(50).HasColumnName("USDACategory");
            });

            modelBuilder.Entity<Apple>(entity => {
                entity.Property(e => e.Variety).HasMaxLength(50).HasColumnName("Variety");
                entity.Property(e => e.Description).HasMaxLength(250).HasColumnName("Description");
                entity.Property(e => e.Photo).HasColumnType("image").HasColumnName("Photo");
            });

            modelBuilder.Entity<Strawberry>(entity => {
                entity.Property(e => e.Variety).HasMaxLength(50).HasColumnName("Variety");
                entity.Property(e => e.Description).HasMaxLength(250).HasColumnName("Description");
                entity.Property(e => e.Photo).HasColumnType("image").HasColumnName("Photo");
            });

            modelBuilder.Entity<Tomato>(entity => {
                entity.Property(e => e.Variety).HasMaxLength(50).HasColumnName("Variety");
                entity.Property(e => e.Description).HasMaxLength(250).HasColumnName("Description");
                entity.Property(e => e.Photo).HasColumnType("image").HasColumnName("Photo");
            });

            modelBuilder.Entity<WhitePotato>(entity => {
                entity.Property(e => e.Variety).HasMaxLength(50).HasColumnName("Variety");
                entity.Property(e => e.Description).HasMaxLength(250).HasColumnName("Description");
                entity.Property(e => e.Photo).HasColumnType("image").HasColumnName("Photo");
                entity.Property(e => e.Eyes).HasMaxLength(20).HasColumnName("Eyes");
                entity.Property(e => e.PrimaryUses).HasMaxLength(50).HasColumnName("PrimaryUses");
                entity.Property(e => e.SkinColor).HasMaxLength(20).HasColumnName("SkinColor");
            });

//#if NETSTANDARD
            modelBuilder.Entity<Fruit>(x => x.HasBaseType<ItemOfProduce>());
            modelBuilder.Entity<Vegetable>(x => x.HasBaseType<ItemOfProduce>());
            modelBuilder.Entity<Apple>(x => x.HasBaseType<Fruit>());
            modelBuilder.Entity<Strawberry>(x => x.HasBaseType<Fruit>());
            modelBuilder.Entity<Tomato>(x => x.HasBaseType<Vegetable>());
            modelBuilder.Entity<WhitePotato>(x => x.HasBaseType<Vegetable>());
//#endif

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

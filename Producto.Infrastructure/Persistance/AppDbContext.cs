using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Producto.Domain.Aggregates;
using Producto.Domain.VO;

namespace Producto.Infrastructure.Persistance
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Productos { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .ToTable("Productos"); // Esto define explícitamente el nombre de la tabla

            modelBuilder.Entity<Product>()
                .HasKey(p => p.Id);

            // Mapeo de Value Objects como strings en la BD
            modelBuilder.Entity<Product>()
                .Property(p => p.Nombre)
                .HasColumnName("Nombre")
                .HasConversion(
                    v => v.Valor, // Cómo se almacena en la DB
                    v => new NombreProductoVO(v) // Cómo se convierte al recuperar
                );

            modelBuilder.Entity<Product>()
                .Property(p => p.PrecioBase)
                .HasColumnName("PrecioBase")
                .HasConversion(
                    v => v.Valor,
                    v => new PrecioBaseVO(v)
                );

            modelBuilder.Entity<Product>()
                .Property(p => p.Categoria)
                .HasConversion(
                    v => v.Valor,
                    v => new CategoriaVO(v)
                );

            modelBuilder.Entity<Product>()
                .Property(p => p.ImagenUrl)
                .HasConversion(
                    v => v.Valor,
                    v => new ImagenUrlVo(v)
                );

            modelBuilder.Entity<Product>()
                .Property(p => p.Estado)
                .HasConversion(v => v.Valor, v => new EstadoVO(v));

            modelBuilder.Entity<Product>().Property(p => p.Id_Usuario)
                .HasConversion(v => v.Valor, v => new Id_Usuario_VO(v));


        }
    }

}

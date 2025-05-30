using System;
using System.Collections.Generic;
using System.Linq; // Necesario para algunos queries de EF Core si se usaran
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Necesario para EntityState y otros métodos de EF Core
using Producto.Domain.Aggregates;
using Producto.Domain.Repositories;
using Producto.Infrastructure.Persistance; // Namespace de AppDbContext

namespace Producto.Infrastructure.Persistance.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly AppDbContext _context;

        public ProductoRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AgregarAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            await _context.Productos.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        // --- IMPLEMENTACIÓN DE ActualizarAsync ---
        public async Task ActualizarAsync(Product producto)
        {
            if (producto == null) throw new ArgumentNullException(nameof(producto));

            // 🔹 Asegurar que la entidad ya está siendo rastreada antes de actualizarla
            var productoExistente = await _context.Productos.FindAsync(producto.Id);
            if (productoExistente != null)
            {
                _context.Entry(productoExistente).CurrentValues.SetValues(producto);
            }
            else
            {
                _context.Productos.Attach(producto);
                _context.Productos.Update(producto);
            }

            await _context.SaveChangesAsync();
        }

        // --- IMPLEMENTACIÓN DE ObtenerPorIdAsync ---
        public async Task<Product> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Productos.FindAsync(id);

        }

        // Los siguientes métodos aún necesitan implementación:

        public Task<List<Product>> ObtenerTodosAsync()
        {
            throw new NotImplementedException("ObtenerTodosAsync no está implementado.");
        }

        // --- IMPLEMENTACIÓN DE Eliminar Producto ---
        public async Task EliminarAsync(Guid id)
        {
            var productoParaEliminar = await _context.Productos.FindAsync(id);
            if (productoParaEliminar != null)
            {
                _context.Productos.Remove(productoParaEliminar);
                await _context.SaveChangesAsync();
            }

        }

    }
}
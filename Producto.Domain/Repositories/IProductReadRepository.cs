// En Producto.Application.Contracts.Persistence/IProductReadRepository.cs (o similar)
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Producto.Domain.Aggregates;


namespace Producto.Application.Contracts.Persistence // O el namespace que elijas
{
    public interface IProductReadRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); // Para la consulta por ID que mencionaste antes
        // Puedes añadir más métodos aquí para otras consultas, ej. por categoría, etc.
    }
}
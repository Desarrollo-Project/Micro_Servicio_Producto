using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Producto.Domain.Aggregates;

namespace Producto.Domain.Repositories
{
    public interface IProductoRepository
    {
        Task AgregarAsync(Product producto);
        Task ActualizarAsync(Product producto);
        Task EliminarAsync(Guid id);
        Task<Product> ObtenerPorIdAsync(Guid id);
    }

}
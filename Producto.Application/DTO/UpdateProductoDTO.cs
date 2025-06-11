using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Application.DTO  
{
    public class UpdateProductoDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = default!;
        public decimal PrecioBase { get; set; }
        public string Categoria { get; set; } = default!;
        public string ImagenUrl { get; set; } = default!;
        public string Estado { get; set; }
        public string Id_Usuario { get; set; }
    }
}

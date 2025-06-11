using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Producto.Application.DTO
{
    public class Get_Productos_DTO
    {
        public string Nombre { get; set; } = default;
        public decimal PrecioBase { get; set; }
        public string Categoria { get; set; } = default;
        public string? ImagenUrl { get; set; }
        public IFormFile IForm { get; set; } = default;
        public string Estado { get; set; } = default;
        public Guid Id_Usuario { get; set; } = default;

    }
}

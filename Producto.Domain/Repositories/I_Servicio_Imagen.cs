using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Producto.Domain.Repositories
{
    public interface I_Servicio_Imagen
    {
        Task<String> Cargar_Imagen(IFormFile Archivo_Imagen);
        Task Eliminar_Imagen(string url_Imagen);

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.Excepciones
{
    public class Excepcion_Imagen_Invalida : Exception
    {
        public Excepcion_Imagen_Invalida(string mensaje) : base(mensaje)
        {
        }
    }


}

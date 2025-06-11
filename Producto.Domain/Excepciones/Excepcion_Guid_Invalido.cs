using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.Excepciones
{
    public class Excepcion_Guid_Invalido : Exception
    {
        public Excepcion_Guid_Invalido(string mensaje) : base(mensaje)
        {
        }
    }
}

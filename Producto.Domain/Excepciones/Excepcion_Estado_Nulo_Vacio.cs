using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.Excepciones
{
    public class Excepcion_Estado_Nulo_Vacio : Exception
    {
        public Excepcion_Estado_Nulo_Vacio(string mensaje) : base(mensaje)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.VO
{
    public class NombreProductoVO
    {
        public string Valor { get; }

        public NombreProductoVO(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("El nombre del producto no puede ser vacio");
            Valor = valor;
        }

        public static implicit operator string(NombreProductoVO nombre) => nombre.Valor;

        public static implicit operator NombreProductoVO(string valor) => new NombreProductoVO(valor);

    }
}
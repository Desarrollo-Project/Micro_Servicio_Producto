using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Producto.Domain.Excepciones;

namespace Producto.Domain.VO
{
    public class EstadoVO
    {
        public string Valor { get; }

        public EstadoVO(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("La categoría no puede estar vacía.");
            Valor = valor;
        }

        public static implicit operator string(EstadoVO estadoVo) => estadoVo.Valor;
        public static implicit operator EstadoVO(string valor) => new EstadoVO(valor);

    }
}

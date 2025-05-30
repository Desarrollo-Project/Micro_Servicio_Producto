using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.VO
{
    public class PrecioBaseVO
    {
        public decimal Valor { get; }

        public PrecioBaseVO(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("El Precio base tiene que ser mayor que cero.");
            Valor = valor;
        }

        public static implicit operator decimal(PrecioBaseVO P) => P.Valor;

        public static implicit operator PrecioBaseVO(decimal valor) => new PrecioBaseVO(valor);

        public static implicit operator PrecioBaseVO(string valor)
        {
            if (decimal.TryParse(valor, out decimal resultado))
            {
                return new PrecioBaseVO(resultado);
            }
            throw new ArgumentException("El valor proporcionado no es un número válido.");
        }
    }
}
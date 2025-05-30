using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.VO
{
    public class CategoriaVO
    {
        public string Valor { get; }

        public CategoriaVO(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("La categoría no puede estar vacía.");
            Valor = valor;
        }

        public static implicit operator string(CategoriaVO cat) => cat.Valor;

        public static implicit operator CategoriaVO(string valor) => new CategoriaVO(valor);
    }
};
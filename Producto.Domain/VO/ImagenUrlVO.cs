using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.VO
{
    public class ImagenUrlVo
    {
        public string Valor { get; }

        public ImagenUrlVo(string valor)
        {
            Valor = valor;
        }

        public static implicit operator string(ImagenUrlVo url) => url.Valor;

        public static implicit operator ImagenUrlVo(string valor) => new ImagenUrlVo(valor);

    }
}
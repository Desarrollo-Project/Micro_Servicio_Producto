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
            if (!Uri.IsWellFormedUriString(valor, UriKind.Absolute))
                throw new ArgumentException("La URL de la imagen no es válida.");
            Valor = valor;
        }

        public static implicit operator string(ImagenUrlVo url) => url.Valor;

        public static implicit operator ImagenUrlVo(string valor) => new ImagenUrlVo(valor);

    }
}
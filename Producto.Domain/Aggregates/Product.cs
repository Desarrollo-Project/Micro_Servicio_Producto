using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Producto.Domain.VO;

namespace Producto.Domain.Aggregates
{
    public class Product
    {
        public Guid Id { get; private set; }
        public NombreProductoVO Nombre { get; private set; }
        public PrecioBaseVO PrecioBase { get; private set; }
        public CategoriaVO Categoria { get; private set; }
        public ImagenUrlVo ImagenUrl { get; private set; }
        public  EstadoVO Estado { get; private set; }
        public Id_Usuario_VO Id_Usuario { get; private set; }

        // Constructor requerido por EF Core
        private Product() { }

        public Product(Guid id, NombreProductoVO nombre, PrecioBaseVO precioBase, CategoriaVO categoria,ImagenUrlVo imagenUrl,EstadoVO estadoVo
            ,Id_Usuario_VO id_Usuario)
        {
            Id = id;
            Nombre = nombre;
            PrecioBase = precioBase;
            Categoria = categoria;
            ImagenUrl = imagenUrl;
            Estado = estadoVo;
            Id_Usuario = id_Usuario;
        }

        // Método para actualizar 
        public void Actualizar(string nombre, decimal precioBase, string categoria, string imagenUrl,string estadoVo, string idUsuario)
        {
            Nombre = new NombreProductoVO(nombre);
            PrecioBase = new PrecioBaseVO(precioBase);
            Categoria = new CategoriaVO(categoria);
            ImagenUrl = new ImagenUrlVo(imagenUrl);
            Estado = new EstadoVO(estadoVo);
            Id_Usuario = new Id_Usuario_VO (idUsuario);

        }
    }

}
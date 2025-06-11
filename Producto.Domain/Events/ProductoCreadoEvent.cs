using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Producto.Domain.VO;

namespace Producto.Domain.Events
{
    public class ProductoCreadoEvent : INotification
    {
        public Guid Id { get; }
        public string Nombre { get; }
        public decimal PrecioBase { get; }
        public string Categoria { get; }
        public string ImagenUrl { get; }
        public string Estado { get;  set; }
        public string Id_Usuario { get;  set; }

        public ProductoCreadoEvent(Guid id, string nombre, decimal precioBase, string categoria, string imagenUrl,String estado,
            string id_usuario)
        {
            Id = id;
            Nombre = nombre;
            PrecioBase = precioBase;
            Categoria = categoria;
            ImagenUrl = imagenUrl;
            Estado = estado;
            Id_Usuario = id_usuario;
        }
    }

}
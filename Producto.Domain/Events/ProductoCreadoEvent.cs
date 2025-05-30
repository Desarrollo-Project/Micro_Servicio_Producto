using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Producto.Domain.Events
{
    public class ProductoCreadoEvent : INotification
    {
        public Guid Id { get; }
        public string Nombre { get; }
        public decimal PrecioBase { get; }
        public string Categoria { get; }
        public string ImagenUrl { get; }

        public ProductoCreadoEvent(Guid id, string nombre, decimal precioBase, string categoria, string imagenUrl)
        {
            Id = id;
            Nombre = nombre;
            PrecioBase = precioBase;
            Categoria = categoria;
            ImagenUrl = imagenUrl;
        }
    }

}
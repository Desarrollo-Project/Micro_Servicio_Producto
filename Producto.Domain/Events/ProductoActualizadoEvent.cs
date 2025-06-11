using MediatR; 
using System;

namespace Producto.Domain.Events
{
    public class ProductoActualizadoEvent : INotification // <-- Añade ": INotification" aquí
    {
        public Guid Id { get; set; }
        public string Id_Usuario { get; set; }

        public string Nombre { get; set; }
        public decimal PrecioBase { get; set; }
        public string Categoria { get; set; }
        public string ImagenUrl { get; set; }
        public string Estado { get; set; }

        public ProductoActualizadoEvent(Guid id, string nombre, decimal precioBase, string categoria, string imagenUrl,string  estado, 
                String id_usuario)
        {
            Id = id;
            Nombre = nombre;
            PrecioBase = precioBase;
            Categoria = categoria;
            ImagenUrl = imagenUrl;
            Estado = estado;
            Id_Usuario = id_usuario;
        }

        // Constructor por default 
        public ProductoActualizadoEvent() { }
    }
}
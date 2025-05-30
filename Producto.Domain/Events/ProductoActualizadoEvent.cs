using MediatR; 
using System;

namespace Producto.Domain.Events
{
    public class ProductoActualizadoEvent : INotification // <-- Añade ": INotification" aquí
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public decimal PrecioBase { get; set; }
        public string Categoria { get; set; }
        public string ImagenUrl { get; set; }

        // Constructor (opcional pero recomendado, como lo hablamos antes)
        public ProductoActualizadoEvent(Guid id, string nombre, decimal precioBase, string categoria, string imagenUrl)
        {
            Id = id;
            Nombre = nombre;
            PrecioBase = precioBase;
            Categoria = categoria;
            ImagenUrl = imagenUrl;
        }

        // Constructor por defecto (puede ser necesario para algunas bibliotecas de deserialización o si usas inicializadores de objeto)
        public ProductoActualizadoEvent() { }
    }
}
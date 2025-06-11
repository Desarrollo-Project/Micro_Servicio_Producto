using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Producto.Domain.Excepciones;
using Producto.Domain.Repositories;
using Producto.Domain.Excepciones;

namespace Producto.Infrastructure.Cloud_Dinary_Configuration
{
    public class Servicio_Imagen: I_Servicio_Imagen
    {

        private readonly CloudinaryDotNet.Cloudinary _cloudinary;

        public Servicio_Imagen(IConfiguration configuracion)
        {
            var account = new Account(
                configuracion["CloudDinary:Cloud_Name"],
                configuracion["CloudDinary:Api_Key"],
                configuracion["CloudDinary:Api_Secret"]);

            _cloudinary = new CloudinaryDotNet.Cloudinary(account);
        }

        public async Task<String> Cargar_Imagen(IFormFile Archivo_Imagen)
        {

            if (Archivo_Imagen == null || Archivo_Imagen.Length == 0)
            {
                throw new Excepcion_Imagen_Invalida("La imagen no es válida");
            }

            var Guid_File = $"{Guid.NewGuid()}_{Archivo_Imagen.FileName}";
            using var stream = Archivo_Imagen.OpenReadStream();

            var Cargar_Parametros = new ImageUploadParams
            {
                File = new FileDescription(Guid_File, stream),
                UseFilename = true,
                UniqueFilename = true, 
                Overwrite = false, 
                PublicId = Guid_File,
                Folder = "Productos"
            };

            var result = await _cloudinary.UploadAsync(Cargar_Parametros);
            return result.SecureUrl.ToString();

        }
        public async Task Eliminar_Imagen(string url_Imagen)
        {
            if(String.IsNullOrWhiteSpace(url_Imagen)) throw new Excepcion_Url_Imagen_Invalida("La URL de la imagen no es válida o no existe");

            var Guid_Imagen = Path.GetFileNameWithoutExtension(new Uri(url_Imagen).AbsolutePath);
            var Eliminar_Parametros = new DeletionParams(Guid_Imagen)
            {
                Invalidate = true
            };

            var resultado_Eliminacion = await _cloudinary.DestroyAsync(Eliminar_Parametros);

            if (resultado_Eliminacion.Result != "ok")
            {
                throw new Excepcion_Url_Imagen_Invalida("No se pudo eliminar la imagen la URL no existe");
            }
            else Console.WriteLine(url_Imagen +" Eliminacino de la imagen completa");
        }
    }
}

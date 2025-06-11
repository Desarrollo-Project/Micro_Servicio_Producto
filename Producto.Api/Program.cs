using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using Producto.Application.Contracts.Persistence;
using Producto.Domain.Repositories;
using Producto.Infrastructure.EventBus.Consumer; // Asegúrate que este namespace sea donde está ProductoCreadoEventConsumerService
using Producto.Infrastructure.EventBus.Events;
using Producto.Infrastructure.Persistance.DataBase; // Para MongoInitializer
using Producto.Infrastructure.Persistance;       // Para AppDbContext
using Producto.Infrastructure.Persistance.Repositories;
using Producto.Application.Handlers;
using CloudinaryDotNet;
using Producto.Infrastructure.Cloud_Dinary_Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios generales
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Microservicio Producto", Version = "v1" });
});

// Registrar autenticación y autorización
builder.Services.AddAuthorization();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProductoCommandHandler).Assembly));

/* Configuracion para Postgres  */
var connectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

/* Configuracion para Mongo */
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var mongoClient = sp.GetRequiredService<IMongoClient>();
    return mongoClient.GetDatabase("productos_db");
});

// Registrar MongoInitializer como Singleton (la ejecución se hará después de app.Build())
builder.Services.AddSingleton<MongoInitializer>();

// 
builder.Services.AddScoped<I_Servicio_Imagen, Servicio_Imagen>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();

builder.Services.AddSingleton<IProductReadRepository, ProductReadRepository>(sp =>
{
    var mongoDatabase = sp.GetRequiredService<IMongoDatabase>();
    return new ProductReadRepository(mongoDatabase);
});


// Configuración de RabbitMQ

/* HACER EXCEPCIONES PERSONALIZADAS */

var rabbitHost = builder.Configuration["RabbitMQ:Host"];
    //?? throw new InvalidOperationException("Falta la configuración RabbitMQ:Host");

    var rabbitUser = builder.Configuration["RabbitMQ:Username"];
   // ?? throw new InvalidOperationException("Falta la configuración RabbitMQ:Username");

   var rabbitPass = builder.Configuration["RabbitMQ:Password"];

   // ?? throw new InvalidOperationException("Falta la configuración RabbitMQ:Password");

// Configurar RabbitMQ Producer (IEventPublisher)
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>(sp =>
    new RabbitMQEventPublisher(rabbitHost, rabbitUser, rabbitPass) // Limpio
);


// Configuracion necesaria para CLoudDInary
builder.Services.AddSingleton<CloudinaryDotNet.Cloudinary>(provider => {
    var config = provider.GetRequiredService<IConfiguration>();
    var account = new Account(
        config["ClouDinary:Cloud_Name"],
        config["ClouDinary:Api_Key"],
        config["ClouDinary:Api_Secret"]
    );
    return new CloudinaryDotNet.Cloudinary(account);
});



// Configuracion para el consumidor de ProductoCreado 
builder.Services.AddSingleton<IHostedService>(sp =>
    new ProductoCreadoEventConsumer( // El consumidor que acabamos de ajustar
        new RabbitMQEventConsumerConnection(rabbitHost, rabbitUser, rabbitPass),
        sp.GetRequiredService<IServiceProvider>()
    )
);

// Configuracion para el consumidor de ProductoActualizadoEvent 
builder.Services.AddSingleton<IHostedService>(sp =>
    new ProductoActualizadoEventConsumer(
        new RabbitMQEventConsumerConnection(rabbitHost, rabbitUser, rabbitPass), // Se crea aquí
        sp.GetRequiredService<IServiceProvider>()
    )
);

// Configuracion para  el consumidor de Producto eliminado 
builder.Services.AddSingleton<IHostedService>(sp =>
    new ProductoEliminadoEventConsumer(
        new RabbitMQEventConsumerConnection(rabbitHost, rabbitUser, rabbitPass), 
        sp.GetRequiredService<IServiceProvider>()
    
    )
);

// Registro de repositorios

// Registro de operaciones de MongoDB
builder.Services.AddScoped<Producto.Infrastructure.Persistance.MongoOperations.MongoCreateProducto>();
builder.Services.AddScoped<Producto.Infrastructure.Persistence.MongoOperations.MongoUpdateProducto>(); 
builder.Services.AddScoped<Producto.Infrastructure.Persistence.MongoOperations.MongoDeleteProducto>(); 


var app = builder.Build();

// Middleware pipeline
// Swagger habilitado para todos los entornos
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Producto API v1");
    // Para acceder a Swagger UI desde la raíz en producción (opcional):
    // c.RoutePrefix = string.Empty;
});

// Condición original para otros middlewares de desarrollo (si los hubiera)
if (app.Environment.IsDevelopment())
{
    // Aquí irían otros middlewares específicos de desarrollo si los tienes
    // Ejemplo: app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Endpoint de Hola Mundo de prueba
app.MapGet("/holamundo", () => "Hola Mundo!");

// Inicialización de MongoDB (Lugar correcto para ejecutarla)
using (var scope = app.Services.CreateScope())
{
    var mongoInitializer = scope.ServiceProvider.GetRequiredService<MongoInitializer>();
    mongoInitializer.Initialize();
}

app.Run();
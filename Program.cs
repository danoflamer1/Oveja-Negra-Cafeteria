using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using OvejaNegra.Context;
using OvejaNegra.Dtos;
using OvejaNegra.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("CadenaConexion");
}

Console.WriteLine($"CONNECTION: {connectionString}");

builder.Services.AddDbContext<MiContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Add services to the container.
builder.Services.AddControllersWithViews();

//cookies 
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.LoginPath = "/Login/Index";
        option.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        option.AccessDeniedPath = "/Home/Index";
    });
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MiContext>();
    context.Database.Migrate();

    if (!context.Categorias.Any())
    {
        context.Categorias.AddRange(
            new Categoria { Nombre = "Destilados Artesanales" },
            new Categoria { Nombre = "Cafes" },
            new Categoria { Nombre = "Bebidas Calientes sin Cafe" },
            new Categoria { Nombre = "Bebidas Frias" },
            new Categoria { Nombre = "Milkshakes" },
            new Categoria { Nombre = "Jugos de Fruta" },
            new Categoria { Nombre = "Frappuccinos" },
            new Categoria { Nombre = "Sandwiches" },
            new Categoria { Nombre = "Pizzas" }
        );
        context.SaveChanges();
    }
    if (!context.Usuarios.Any())
    {
        context.Usuarios.AddRange(
            new Usuario
            {
                Nombre = "Admin",
                Apellido = "Admin",
                username = "admin",
                password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Rol = Rolenum.Administrador
            },
            new Usuario
            {
                Nombre = "Cajero",
                Apellido = "Cajero",
                username = "cajero",
                password = BCrypt.Net.BCrypt.HashPassword("cajero123"),
                Rol = Rolenum.Cajero
            },
            new Usuario
            {
                Nombre = "Mesero",
                Apellido = "Mesero",
                username = "mesero",
                password = BCrypt.Net.BCrypt.HashPassword("mesero123"),
                Rol = Rolenum.Mesero
            }
        );
        context.SaveChanges();
    }
    if (!context.Productoss.Any())
    {
        var destilados = context.Categorias.First(c => c.Nombre == "Destilados Artesanales").Id;
        var cafes = context.Categorias.First(c => c.Nombre == "Cafes").Id;
        var calientes = context.Categorias.First(c => c.Nombre == "Bebidas Calientes sin Cafe").Id;
        var frias = context.Categorias.First(c => c.Nombre == "Bebidas Frias").Id;
        var milkshakes = context.Categorias.First(c => c.Nombre == "Milkshakes").Id;
        var jugos = context.Categorias.First(c => c.Nombre == "Jugos de Fruta").Id;
        var frappuccinos = context.Categorias.First(c => c.Nombre == "Frappuccinos").Id;
        var sandwiches = context.Categorias.First(c => c.Nombre == "Sándwiches").Id;
        var pizzas = context.Categorias.First(c => c.Nombre == "Pizzas").Id;

        context.Productoss.AddRange(

            // Destilados Artesanales
            new Producto { Nombre = "Singani Artesanal", Precio = 25.00m, Disponible = true, Descripcion = "Singani artesanal de los Yungas", CategoriaId = destilados },
            new Producto { Nombre = "Cocktail de Singani", Precio = 30.00m, Disponible = true, Descripcion = "Cocktail preparado con singani y frutas", CategoriaId = destilados },
            new Producto { Nombre = "Licor de Cafe", Precio = 28.00m, Disponible = true, Descripcion = "Licor artesanal con base de cafe", CategoriaId = destilados },

            // Cafés
            new Producto { Nombre = "Café Americano", Precio = 12.00m, Disponible = true, Descripcion = "Café negro suave y aromatico", CategoriaId = cafes },
            new Producto { Nombre = "Café Espresso", Precio = 10.00m, Disponible = true, Descripcion = "Espresso concentrado de grano selecto", CategoriaId = cafes },
            new Producto { Nombre = "Cappuccino", Precio = 15.00m, Disponible = true, Descripcion = "Espresso con leche vaporizada y espuma", CategoriaId = cafes },
            new Producto { Nombre = "Latte", Precio = 16.00m, Disponible = true, Descripcion = "Cafe suave con abundante leche cremosa", CategoriaId = cafes },
            new Producto { Nombre = "Macchiato", Precio = 14.00m, Disponible = true, Descripcion = "Espresso con toque de leche vaporizada", CategoriaId = cafes },

            // Bebidas Calientes sin Café
            new Producto { Nombre = "Chocolate Caliente", Precio = 14.00m, Disponible = true, Descripcion = "Chocolate cremoso con leche caliente", CategoriaId = calientes },
            new Producto { Nombre = "Te de Manzanilla", Precio = 10.00m, Disponible = true, Descripcion = "Infusion relajante de manzanilla", CategoriaId = calientes },
            new Producto { Nombre = "Te Verde", Precio = 10.00m, Disponible = true, Descripcion = "Te verde antioxidante importado", CategoriaId = calientes },
            new Producto { Nombre = "Leche con Toddy", Precio = 12.00m, Disponible = true, Descripcion = "Leche caliente con Toddy", CategoriaId = calientes },

            // Bebidas Frías
            new Producto { Nombre = "Aquarius", Precio = 8.00m, Disponible = true, Descripcion = "Aquarius 500ml", CategoriaId = frias },
            new Producto { Nombre = "Coca cola", Precio = 10.00m, Disponible = true, Descripcion = "Coca Cola en botella", CategoriaId = frias },
            new Producto { Nombre = "Limonada", Precio = 12.00m, Disponible = true, Descripcion = "Limonada fresca con hierbabuena", CategoriaId = frias },
            new Producto { Nombre = "Te Frio", Precio = 12.00m, Disponible = true, Descripcion = "Te frío con hielo", CategoriaId = frias },

            // Milkshakes
            new Producto { Nombre = "Milkshake de Fresa", Precio = 20.00m, Disponible = true, Descripcion = "Batido cremoso de fresa con helado", CategoriaId = milkshakes },
            new Producto { Nombre = "Milkshake de Vainilla", Precio = 20.00m, Disponible = true, Descripcion = "Batido clasico de vainilla con crema", CategoriaId = milkshakes },
            new Producto { Nombre = "Milkshake de Chocolate", Precio = 22.00m, Disponible = true, Descripcion = "Batido intenso de chocolate con helado", CategoriaId = milkshakes },
            new Producto { Nombre = "Milkshake de Oreo", Precio = 24.00m, Disponible = true, Descripcion = "Batido de vainilla con trozos de Oreo", CategoriaId = milkshakes },

            // Jugos de Fruta
            new Producto { Nombre = "Jugo de Naranja", Precio = 14.00m, Disponible = true, Descripcion = "Jugo natural de naranja recien exprimido", CategoriaId = jugos },
            new Producto { Nombre = "Jugo de Mango", Precio = 14.00m, Disponible = true, Descripcion = "Jugo natural de mango de temporada", CategoriaId = jugos },
            new Producto { Nombre = "Jugo de Maracuyá", Precio = 14.00m, Disponible = true, Descripcion = "Jugo tropical de maracuya con azucar", CategoriaId = jugos },
            new Producto { Nombre = "Jugo Mixto", Precio = 16.00m, Disponible = true, Descripcion = "Mezcla de frutas de temporada", CategoriaId = jugos },

            // Frappuccinos
            new Producto { Nombre = "Frappuccino de Cafe", Precio = 22.00m, Disponible = true, Descripcion = "Cafe frío batido con hielo y crema", CategoriaId = frappuccinos },
            new Producto { Nombre = "Frappuccino de Caramelo", Precio = 24.00m, Disponible = true, Descripcion = "Frappuccino con salsa de caramelo", CategoriaId = frappuccinos },
            new Producto { Nombre = "Frappuccino de Moca", Precio = 24.00m, Disponible = true, Descripcion = "Frappuccino de café con chocolate", CategoriaId = frappuccinos },
            new Producto { Nombre = "Frappuccino de Vainilla", Precio = 22.00m, Disponible = true, Descripcion = "Frappuccino cremoso sabor vainilla", CategoriaId = frappuccinos },

            // Sándwiches
            new Producto { Nombre = "Sándwich de Pollo", Precio = 25.00m, Disponible = true, Descripcion = "Sándwich de pollo a la plancha con lechuga", CategoriaId = sandwiches },
            new Producto { Nombre = "Sándwich Mixto", Precio = 22.00m, Disponible = true, Descripcion = "Jamón, queso y tomate en pan tostado", CategoriaId = sandwiches },
            new Producto { Nombre = "Sándwich Vegetal", Precio = 20.00m, Disponible = true, Descripcion = "Verduras frescas con queso crema", CategoriaId = sandwiches },
            new Producto { Nombre = "Sandwich Neto", Precio = 28.00m, Disponible = true, Descripcion = "Triple capa con pollo, tocino y huevo", CategoriaId = sandwiches },

            // Pizzas
            new Producto { Nombre = "Pizza Margherita", Precio = 45.00m, Disponible = true, Descripcion = "Salsa de tomate, mozzarella y albahaca", CategoriaId = pizzas },
            new Producto { Nombre = "Pizza de Pepperoni", Precio = 50.00m, Disponible = true, Descripcion = "Pizza clásica con pepperoni y queso", CategoriaId = pizzas },
            new Producto { Nombre = "Pizza Vegetariana", Precio = 48.00m, Disponible = true, Descripcion = "Pimientos, champiñones, aceitunas y queso", CategoriaId = pizzas },
            new Producto { Nombre = "Pizza de Charque", Precio = 52.00m, Disponible = true, Descripcion = "Charque con pimenton", CategoriaId = pizzas }
        );
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();

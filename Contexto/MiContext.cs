using Microsoft.EntityFrameworkCore;
using OvejaNegra.Models;

namespace OvejaNegra.Context
{
    public class MiContext : DbContext
    {
        public MiContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Comanda> Comandas { get; set; }
        public DbSet<DetalleComanda> DetalleComandas { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<Producto> Productoss { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .Property(u => u.Rol)
                .HasConversion<string>();
            modelBuilder.Entity<Comanda>()
                .Property(u => u.Estado)
                .HasConversion<string>();
            modelBuilder.Entity<Venta>()
                .Property(u => u.MetodoPago)
                .HasConversion<string>();
        }
    }
}

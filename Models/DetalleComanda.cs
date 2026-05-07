using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OvejaNegra.Models
{
    public class DetalleComanda
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Cantidad")]
        public int Cantidad { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [DisplayName("Precio Unitario")] 
        public decimal Precio_unitario { get; set; }
        [DisplayName("Observaciones")]
        public string? Observacion { get; set; }
        //Relaciones
        public int ComandaId { get; set; }
        public virtual Comanda? Comanda { get; set; }

        public int ProductoId { get; set; }
        public virtual Producto? Producto { get; set; }
    }
}

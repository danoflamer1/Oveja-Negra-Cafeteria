using System.ComponentModel.DataAnnotations;

namespace OvejaNegra.Models
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }
        [Required, MinLength(2), MaxLength(50)]
        [Display(Name = "Nombre del cliente")]
        public string? Nombre { get; set; }
        [Display(Name = "NIT o CI del cliente")]
        [RegularExpression(@"^\d{8,12}$", ErrorMessage = "El NIT o CI debe tener entre 8 y 12 numeros")]
        public string? NITCI { get; set; }

        //Relaciones
        public virtual List<Venta>? Ventas { get; set; }
    }
}
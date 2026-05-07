using System.ComponentModel.DataAnnotations;

namespace OvejaNegra.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }
        [Required, MinLength(2), MaxLength(50)]
        [Display(Name = "Nombre de la categoría")]
        public string? Nombre { get; set; }
        //Relaciones
        public virtual List<Producto>? Productos { get; set; }
    }
}

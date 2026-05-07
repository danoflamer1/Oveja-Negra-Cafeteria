namespace OvejaNegra.Dtos
{
    public enum Rolenum
    {
        Administrador = 1,
        Cajero = 2,
        Mesero = 3
    }
    public class CambiarEstadoDTO
    {
        public int ComandaId { get; set; }
        public int Estado { get; set; }
    }
}

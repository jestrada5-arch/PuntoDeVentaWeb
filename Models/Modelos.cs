using System.ComponentModel.DataAnnotations;

namespace PuntoVentaWeb.Models
{
    // ---------- Autenticación ----------
    public class LoginModelo
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El usuario admite máximo 50 caracteres.")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La contraseña admite máximo 100 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRespuesta
    {
        public string Token { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
    }

    // ---------- Menú dinámico ----------
    public class MenuDto
    {
        public int IdMenu { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icono { get; set; }
        public List<MenuDto> Hijos { get; set; } = new();
    }

    // ---------- Puestos ----------
    public class PuestoDto
    {
        public short IdPuesto { get; set; }

        [Required(ErrorMessage = "El puesto es obligatorio.")]
        [StringLength(50, ErrorMessage = "El puesto admite máximo 50 caracteres.")]
        public string Puesto { get; set; } = string.Empty;

        public PuestoDto Clonar() => (PuestoDto)MemberwiseClone();
    }

    // ---------- Marcas ----------
    public class MarcaDto
    {
        public short IdMarca { get; set; }

        [Required(ErrorMessage = "La marca es obligatoria.")]
        [StringLength(50, ErrorMessage = "La marca admite máximo 50 caracteres.")]
        public string Marca { get; set; } = string.Empty;

        public MarcaDto Clonar() => (MarcaDto)MemberwiseClone();
    }

    // ---------- Clientes ----------
    public class ClienteDto
    {
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(60, ErrorMessage = "Los nombres admiten máximo 60 caracteres.")]
        [RegularExpression(@"^\p{L}[\p{L} .'\-]*\z", ErrorMessage = "Los nombres solo admiten letras.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(60, ErrorMessage = "Los apellidos admiten máximo 60 caracteres.")]
        [RegularExpression(@"^\p{L}[\p{L} .'\-]*\z", ErrorMessage = "Los apellidos solo admiten letras.")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El NIT es obligatorio.")]
        [StringLength(12, ErrorMessage = "El NIT admite máximo 12 caracteres.")]
        [RegularExpression(@"^\d{1,10}-?[0-9Kk]\z", ErrorMessage = "NIT inválido (ejemplo: 123456-7).")]
        public string Nit { get; set; } = string.Empty;

        // true = masculino, false = femenino
        public bool Genero { get; set; }

        [RegularExpression(@"^\d{8,12}\z", ErrorMessage = "El teléfono debe tener entre 8 y 12 dígitos.")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [StringLength(45, ErrorMessage = "El correo admite máximo 45 caracteres.")]
        public string? CorreoElectronico { get; set; }

        public DateTime FechaIngreso { get; set; }

        public ClienteDto Clonar() => (ClienteDto)MemberwiseClone();
    }
}

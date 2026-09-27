using System.ComponentModel.DataAnnotations;

namespace Products.API.DTOs;

public record CreateProductRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; init; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal? Precio { get; init; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
    public int? Stock { get; init; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; init; } = string.Empty;
}

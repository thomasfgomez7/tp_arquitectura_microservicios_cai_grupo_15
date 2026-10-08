namespace Cart.API.Exceptions;

/// <summary>
/// Catálogo de códigos de error de Cart.API (sección 4.4 del enunciado).
/// </summary>
public static class ErrorCodes
{
    /// <summary>404 — Carrito no encontrado.</summary>
    public const string CRT_001 = "CRT-001";

    /// <summary>404 — Producto no encontrado (en Products.API o en el carrito, D-26).</summary>
    public const string CRT_002 = "CRT-002";

    /// <summary>422 — Stock insuficiente para agregar al carrito.</summary>
    public const string CRT_003 = "CRT-003";

    /// <summary>400 — Cantidad inválida; es el único 400 del catálogo, se usa para todo dato inválido (D-25).</summary>
    public const string CRT_004 = "CRT-004";

    /// <summary>500 — Error interno al procesar el carrito.</summary>
    public const string CRT_005 = "CRT-005";
}

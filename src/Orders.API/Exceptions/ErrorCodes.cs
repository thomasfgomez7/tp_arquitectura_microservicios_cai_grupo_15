namespace Orders.API.Exceptions;

/// <summary>
/// Catálogo de códigos de error de Orders.API (sección 4.3 del enunciado).
/// </summary>
public static class ErrorCodes
{
    /// <summary>404 — Orden no encontrada.</summary>
    public const string ORD_001 = "ORD-001";

    /// <summary>400 — Los datos de la orden son inválidos (campos faltantes, sin items o estado desconocido).</summary>
    public const string ORD_002 = "ORD-002";

    /// <summary>404 — Usuario no encontrado al crear la orden.</summary>
    public const string ORD_003 = "ORD-003";

    /// <summary>404 — Producto no encontrado al crear la orden.</summary>
    public const string ORD_004 = "ORD-004";

    /// <summary>422 — Stock insuficiente para uno o más productos.</summary>
    public const string ORD_005 = "ORD-005";

    /// <summary>409 — El estado de la orden no puede ser modificado (transición inválida).</summary>
    public const string ORD_006 = "ORD-006";

    /// <summary>500 — Error interno al procesar la orden.</summary>
    public const string ORD_007 = "ORD-007";
}

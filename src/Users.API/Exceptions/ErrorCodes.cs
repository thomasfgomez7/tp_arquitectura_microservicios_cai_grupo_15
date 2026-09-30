namespace Users.API.Exceptions;

public static class ErrorCodes
{
    public const string USR_001 = "USR-001"; // Email ya registrado
    public const string USR_002 = "USR-002"; // Datos inválidos
    public const string USR_003 = "USR-003"; // Credenciales incorrectas
    public const string USR_004 = "USR-004"; // Bloqueado por intentos
    public const string USR_005 = "USR-005"; // Bloqueado por fraude
    public const string USR_006 = "USR-006"; // Error interno
}
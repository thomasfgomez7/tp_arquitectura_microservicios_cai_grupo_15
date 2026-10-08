using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.ExceptionHandlers;

namespace Notifications.API.Infrastructure;

/// <summary>
/// Documenta en Swagger una respuesta de error del catálogo: declara el status con el tipo
/// <see cref="ErrorResponse"/> (como ProducesResponseType) y guarda el código y el mensaje para que
/// <see cref="ErrorExamplesOperationFilter"/> arme el ejemplo.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ProducesErrorAttribute(int statusCode, string errorCode, string errorMessage)
    : ProducesResponseTypeAttribute(typeof(ErrorResponse), statusCode, ErrorResponseWriter.ContentType)
{
    public string ErrorCode { get; } = errorCode;

    public string ErrorMessage { get; } = errorMessage;
}

using Orders.API.Models;
using Orders.API.Services;

namespace Orders.API.Tests.Unit.Services;

/// <summary>
/// Recorre las 25 combinaciones de estado actual y estado nuevo: 5 válidas y 20 inválidas.
/// </summary>
public class OrderStatusTransitionsTests
{
    [Theory]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Confirmada)]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Cancelada)]
    [InlineData(OrderStatus.Confirmada, OrderStatus.Enviada)]
    [InlineData(OrderStatus.Confirmada, OrderStatus.Cancelada)]
    [InlineData(OrderStatus.Enviada, OrderStatus.Entregada)]
    public void CanTransition_TransicionValida_DevuelveTrue(OrderStatus desde, OrderStatus hacia)
    {
        Assert.True(OrderStatusTransitions.CanTransition(desde, hacia));
    }

    [Theory]
    // Quedar en el mismo estado no es un cambio válido.
    [InlineData(OrderStatus.Pendiente, OrderStatus.Pendiente)]
    [InlineData(OrderStatus.Confirmada, OrderStatus.Confirmada)]
    [InlineData(OrderStatus.Enviada, OrderStatus.Enviada)]
    [InlineData(OrderStatus.Entregada, OrderStatus.Entregada)]
    [InlineData(OrderStatus.Cancelada, OrderStatus.Cancelada)]
    // Saltearse pasos.
    [InlineData(OrderStatus.Pendiente, OrderStatus.Enviada)]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Entregada)]
    [InlineData(OrderStatus.Confirmada, OrderStatus.Entregada)]
    // Volver hacia atrás.
    [InlineData(OrderStatus.Confirmada, OrderStatus.Pendiente)]
    [InlineData(OrderStatus.Enviada, OrderStatus.Pendiente)]
    [InlineData(OrderStatus.Enviada, OrderStatus.Confirmada)]
    // Una orden enviada ya no se puede cancelar.
    [InlineData(OrderStatus.Enviada, OrderStatus.Cancelada)]
    // Entregada y Cancelada son estados finales.
    [InlineData(OrderStatus.Entregada, OrderStatus.Pendiente)]
    [InlineData(OrderStatus.Entregada, OrderStatus.Confirmada)]
    [InlineData(OrderStatus.Entregada, OrderStatus.Enviada)]
    [InlineData(OrderStatus.Entregada, OrderStatus.Cancelada)]
    [InlineData(OrderStatus.Cancelada, OrderStatus.Pendiente)]
    [InlineData(OrderStatus.Cancelada, OrderStatus.Confirmada)]
    [InlineData(OrderStatus.Cancelada, OrderStatus.Enviada)]
    [InlineData(OrderStatus.Cancelada, OrderStatus.Entregada)]
    public void CanTransition_TransicionInvalida_DevuelveFalse(OrderStatus desde, OrderStatus hacia)
    {
        Assert.False(OrderStatusTransitions.CanTransition(desde, hacia));
    }
}

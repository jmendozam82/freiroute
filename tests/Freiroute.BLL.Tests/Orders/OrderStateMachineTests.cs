using System;
using FluentAssertions;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Orders;
using Xunit;

namespace Freiroute.BLL.Tests.Orders;

public class OrderStateMachineTests
{
    // Transiciones VÁLIDAS — una por cada flecha del ADR-019
    [Theory]
    [InlineData("DRAFT",            "CONFIRMED")]
    [InlineData("DRAFT",            "CANCELLED")]
    [InlineData("CONFIRMED",        "ASSIGNED")]
    [InlineData("CONFIRMED",        "ON_HOLD")]
    [InlineData("CONFIRMED",        "PARTIALLY_SPLIT")]
    [InlineData("CONFIRMED",        "CANCELLED")]
    [InlineData("ASSIGNED",         "PICKUP_SCHEDULED")]
    [InlineData("ASSIGNED",         "ON_HOLD")]
    [InlineData("ASSIGNED",         "CANCELLED")]
    [InlineData("PICKUP_SCHEDULED", "IN_TRANSIT")]
    [InlineData("PICKUP_SCHEDULED", "ON_HOLD")]
    [InlineData("IN_TRANSIT",       "DELIVERED")]
    [InlineData("IN_TRANSIT",       "FAILED_DELIVERY")]
    [InlineData("IN_TRANSIT",       "ON_HOLD")]
    [InlineData("DELIVERED",        "INVOICED")]
    [InlineData("INVOICED",         "CLOSED")]
    [InlineData("ON_HOLD",          "CONFIRMED")]
    [InlineData("FAILED_DELIVERY",  "IN_TRANSIT")]
    public void CanTransition_TransicionValida_RetornaTrue(string from, string to)
    {
        OrderStateMachine.CanTransition(from, to).Should().BeTrue(
            $"la transición {from} → {to} debe ser válida según ADR-019");
    }

    // Transiciones INVÁLIDAS — muestras representativas de cada estado
    [Theory]
    // Desde DRAFT — no puede saltar estados
    [InlineData("DRAFT",     "ASSIGNED")]
    [InlineData("DRAFT",     "IN_TRANSIT")]
    [InlineData("DRAFT",     "DELIVERED")]
    [InlineData("DRAFT",     "ON_HOLD")]     // ON_HOLD no aplica desde DRAFT
    // Desde CONFIRMED — no puede retroceder
    [InlineData("CONFIRMED", "DRAFT")]
    // Desde estados terminales — ninguna transición
    [InlineData("CLOSED",    "DRAFT")]
    [InlineData("CLOSED",    "CONFIRMED")]
    [InlineData("CLOSED",    "IN_TRANSIT")]
    [InlineData("CANCELLED", "CONFIRMED")]
    [InlineData("CANCELLED", "DRAFT")]
    // Desde DELIVERED — no puede cancelar
    [InlineData("DELIVERED", "CANCELLED")]
    [InlineData("DELIVERED", "ON_HOLD")]
    // Desde PARTIALLY_SPLIT — sin transiciones directas
    [InlineData("PARTIALLY_SPLIT", "CONFIRMED")]
    [InlineData("PARTIALLY_SPLIT", "IN_TRANSIT")]
    public void CanTransition_TransicionInvalida_RetornaFalse(string from, string to)
    {
        OrderStateMachine.CanTransition(from, to).Should().BeFalse(
            $"la transición {from} → {to} NO debe ser válida según ADR-019");
    }

    // AssertTransition lanza BusinessException
    [Fact]
    public void AssertTransition_TransicionInvalida_LanzaBusinessException()
    {
        // Arrange
        var from = OrdenEstado.Draft;
        var to   = OrdenEstado.Delivered;

        // Act & Assert
        var act = () => OrderStateMachine.AssertTransition(from, to);
        act.Should().Throw<BusinessException>()
            .WithMessage("*Borrador*Entregada*");
    }

    [Fact]
    public void AssertTransition_TransicionValida_NoLanzaExcepcion()
    {
        var act = () => OrderStateMachine.AssertTransition(
            OrdenEstado.Draft, OrdenEstado.Confirmed);
        act.Should().NotThrow();
    }

    // Estados terminales — sin transiciones
    [Theory]
    [InlineData("CLOSED")]
    [InlineData("CANCELLED")]
    public void GetNextStates_EstadoTerminal_RetornaSetVacio(string estado)
    {
        var next = OrderStateMachine.GetNextStates(estado);
        next.Should().BeEmpty(
            $"{estado} es estado terminal — no admite transiciones");
    }

    // GetNextStates retorna las transiciones correctas
    [Fact]
    public void GetNextStates_Confirmed_RetornaTransicionesCorrectas()
    {
        var next = OrderStateMachine.GetNextStates(OrdenEstado.Confirmed);
        next.Should().BeEquivalentTo(new[]
        {
            OrdenEstado.Assigned,
            OrdenEstado.OnHold,
            OrdenEstado.PartiallySplit,
            OrdenEstado.Cancelled
        });
    }

    [Fact]
    public void GetNextStates_EstadoDesconocido_RetornaSetVacio()
    {
        var next = OrderStateMachine.GetNextStates("ESTADO_INEXISTENTE");
        next.Should().BeEmpty();
    }

    // AssertTransition con estados desconocidos también lanza BusinessException
    [Fact]
    public void AssertTransition_EstadoOrigenDesconocido_LanzaBusinessException()
    {
        var act = () => OrderStateMachine.AssertTransition("ESTADO_INEXISTENTE", OrdenEstado.Delivered);
        act.Should().Throw<BusinessException>()
            .WithMessage("*Transición de estado inválida*");
    }
}



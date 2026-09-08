using System;
using System.Linq;
using FluentAssertions;
using FluentValidation.Results;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;
using Freiroute.BLL.Tests.Builders;
using Xunit;

namespace Freiroute.BLL.Tests.Validators;

public class OrdenValidatorTests
{
    private readonly OrdenValidator _validator;

    public OrdenValidatorTests()
    {
        _validator = new OrdenValidator();
    }

    private OrdenRequestDto DtoValido() => new OrdenBuilder().BuildRequestDto();

    // ── Casos VÁLIDOS ──────────────────────────────────────────────────
    [Fact]
    public void Validate_CuandoDtoValido_NoHayErrores()
    {
        var dto = DtoValido();
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    // ── Campos obligatorios vacíos ─────────────────────────────────────
    [Fact]
    public void Validate_CuandoClienteIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.ClienteId = Guid.Empty;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClienteId");
    }

    [Fact]
    public void Validate_CuandoOrigenIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.OrigenId = Guid.Empty;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrigenId");
    }

    [Fact]
    public void Validate_CuandoDestinoIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.DestinoId = Guid.Empty;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinoId");
    }

    [Fact]
    public void Validate_CuandoTipoMercanciaIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.TipoMercanciaId = Guid.Empty;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoMercanciaId");
    }

    [Fact]
    public void Validate_CuandoUnidadMedidaIdVacia_TieneError()
    {
        var dto = DtoValido();
        dto.UnidadMedidaId = Guid.Empty;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnidadMedidaId");
    }

    // ── CA-07: Origen == Destino ───────────────────────────────────────
    [Fact]
    public void Validate_CuandoOrigenIgualDestino_TieneErrorDescriptivo()
    {
        var mismoId = Guid.NewGuid();
        var dto = DtoValido();
        dto.OrigenId = mismoId;
        dto.DestinoId = mismoId;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("no pueden ser la misma"));
    }

    // ── CA-04: Peso ≤ 0 ───────────────────────────────────────────────
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Validate_CuandoPesoMenorOIgualCero_TieneError(decimal peso)
    {
        var dto = DtoValido();
        dto.PesoKg = peso;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PesoKg");
    }

    // ── CA-05: Cantidad ≤ 0 ───────────────────────────────────────────
    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    public void Validate_CuandoCantidadMenorOIgualCero_TieneError(decimal cantidad)
    {
        var dto = DtoValido();
        dto.Cantidad = cantidad;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cantidad");
    }

    // ── CA-06: Fecha entrega anterior a pickup ────────────────────────
    [Fact]
    public void Validate_CuandoFechaEntregaAntesDeFechaPickup_TieneError()
    {
        var dto = DtoValido();
        dto.FechaPickupSolicitada = new DateOnly(2026, 5, 10);
        dto.FechaEntregaRequerida = new DateOnly(2026, 5, 9);  // antes
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FechaEntregaRequerida");
    }

    [Fact]
    public void Validate_CuandoFechasIguales_EsValido()
    {
        var dto = DtoValido();
        dto.FechaPickupSolicitada = new DateOnly(2026, 5, 10);
        dto.FechaEntregaRequerida = new DateOnly(2026, 5, 10);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CuandoSoloFechaPickup_EsValido()
    {
        var dto = DtoValido();
        dto.FechaPickupSolicitada = new DateOnly(2026, 5, 10);
        dto.FechaEntregaRequerida = null;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    // ── CA-09: ModoTransporte ─────────────────────────────────────────
    [Theory]
    [InlineData("TERRESTRE")]
    [InlineData("AEREO")]
    [InlineData("MARITIMO")]
    [InlineData("FERROVIARIO")]
    [InlineData("INTERMODAL")]
    public void Validate_CuandoModoTransporteValido_NoHayError(string modo)
    {
        var dto = DtoValido();
        dto.ModoTransporte = modo;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("FTL")]           // de Sprint 3, no válido en órdenes
    [InlineData("LTL")]
    [InlineData("INVALIDO")]
    [InlineData("")]
    public void Validate_CuandoModoTransporteInvalido_TieneError(string modo)
    {
        var dto = DtoValido();
        dto.ModoTransporte = modo;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ModoTransporte");
    }

    // ── CA-10: NivelServicio ──────────────────────────────────────────
    [Theory]
    [InlineData("ESTANDAR")]
    [InlineData("EXPRESS")]
    [InlineData("PROGRAMADO")]
    public void Validate_CuandoNivelServicioValido_NoHayError(string nivel)
    {
        var dto = DtoValido();
        dto.NivelServicio = nivel;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("URGENTE")]
    public void Validate_CuandoNivelServicioInvalido_TieneError(string nivel)
    {
        var dto = DtoValido();
        dto.NivelServicio = nivel;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NivelServicio");
    }

    // ── CA-11: Prioridad ──────────────────────────────────────────────
    [Theory]
    [InlineData("CRITICO")]
    [InlineData("ALTO")]
    [InlineData("NORMAL")]
    [InlineData("BAJO")]
    public void Validate_CuandoPrioridadValida_NoHayError(string prioridad)
    {
        var dto = DtoValido();
        dto.Prioridad = prioridad;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("MEDIO")]
    public void Validate_CuandoPrioridadInvalida_TieneError(string prioridad)
    {
        var dto = DtoValido();
        dto.Prioridad = prioridad;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prioridad");
    }

    // ── Líneas ────────────────────────────────────────────────────────
    [Fact]
    public void Validate_CuandoLineaSinDescripcion_TieneError()
    {
        var dto = DtoValido();
        dto.Lineas.First().Descripcion = "";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Descripcion"));
    }

    [Fact]
    public void Validate_CuandoLineaCantidadCero_TieneError()
    {
        var dto = DtoValido();
        dto.Lineas.First().Cantidad = 0;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Cantidad"));
    }
}


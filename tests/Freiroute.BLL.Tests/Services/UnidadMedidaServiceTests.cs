using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Unidad;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de la lógica de negocio de unidades de medida (HU-018).
/// Cubre el simulador de conversión (CA-05), unicidad de símbolo
/// (Conflict) y el ciclo CRUD con auditoría.
/// </summary>
public class UnidadMedidaServiceTests
{
    private readonly Mock<IUnidadMedidaRepository> _repo;
    private readonly Mock<IValidator<UnidadMedidaRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly UnidadMedidaService _service;

    public UnidadMedidaServiceTests()
    {
        _repo = new Mock<IUnidadMedidaRepository>();
        _validator = new Mock<IValidator<UnidadMedidaRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<UnidadMedidaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new UnidadMedidaService(
            _repo.Object, _validator.Object, _auditoria.Object,
            Mock.Of<ILogger<UnidadMedidaService>>());
    }

    private static UnidadMedida Unidad(string simbolo, string tipo, decimal factor, bool activa = true)
        => new()
        {
            Id = Guid.NewGuid(),
            EmpresaId = Guid.NewGuid(),
            Nombre = simbolo,
            Simbolo = simbolo,
            Tipo = tipo,
            FactorConversion = factor,
            UnidadBase = tipo == TipoMedida.Peso ? "kg"
                : tipo == TipoMedida.Volumen ? "m3"
                : tipo == TipoMedida.Longitud ? "m" : "C",
            Activo = activa,
            FechaCreacion = DateTime.UtcNow
        };

    private UnidadMedidaRequestDto DtoValido() => new()
    {
        Nombre = "Libra",
        Simbolo = "lb",
        Tipo = TipoMedida.Peso,
        FactorConversion = 2.2046226m,
        UnidadBase = "kg"
    };

    // ── ConvertirAsync (CA-05) ─────────────────────────────────

    [Fact]
    public async Task ConvertirAsync_KgALibra_CalculaFactorCorrecto()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetBySimboloAsync("kg", empresaId))
            .ReturnsAsync(Unidad("kg", TipoMedida.Peso, 1m));
        _repo.Setup(r => r.GetBySimboloAsync("lb", empresaId))
            .ReturnsAsync(Unidad("lb", TipoMedida.Peso, 2.2046226m));

        var resultado = await _service.ConvertirAsync(100m, "kg", "lb", empresaId);

        // 100 × (1 / 2.2046226) = 45.359237... → 45.3592 (4 decimales, AwayFromZero).
        resultado.Should().Be(45.3592m);
    }

    [Fact]
    public async Task ConvertirAsync_SimboloInexistente_LanzaNotFound()
    {
        _repo.Setup(r => r.GetBySimboloAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync((UnidadMedida?)null);

        var act = async () => await _service.ConvertirAsync(10m, "zzz", "kg", Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ConvertirAsync_TiposDistintos_LanzaBusinessException()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetBySimboloAsync("kg", empresaId))
            .ReturnsAsync(Unidad("kg", TipoMedida.Peso, 1m));
        _repo.Setup(r => r.GetBySimboloAsync("m3", empresaId))
            .ReturnsAsync(Unidad("m3", TipoMedida.Volumen, 1m));

        var act = async () => await _service.ConvertirAsync(10m, "kg", "m3", empresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Code.Should().Be("UNIDAD_TIPOS_DISTINTOS");
    }

    // ── CreateAsync ────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SimboloDuplicado_LanzaConflict()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetBySimboloAsync("lb", empresaId))
            .ReturnsAsync(Unidad("lb", TipoMedida.Peso, 1m));

        var act = async () => await _service.CreateAsync(DtoValido(), empresaId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_ValidadorInvalido_LanzaValidationException()
    {
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<UnidadMedidaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure("Simbolo", "El símbolo es obligatorio")
            ]));

        var act = async () => await _service.CreateAsync(DtoValido(), Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_Exitoso_PersisteYAudita()
    {
        var empresaId = Guid.NewGuid();
        var idCreado = Guid.NewGuid();
        _repo.Setup(r => r.GetBySimboloAsync(It.IsAny<string>(), empresaId))
            .ReturnsAsync((UnidadMedida?)null);
        _repo.Setup(r => r.CreateAsync(It.IsAny<UnidadMedida>())).ReturnsAsync(idCreado);
        var respuesta = Unidad("lb", TipoMedida.Peso, 2.2046226m);
        respuesta.Id = idCreado;
        _repo.Setup(r => r.GetByIdAsync(idCreado, empresaId)).ReturnsAsync(respuesta);

        var resultado = await _service.CreateAsync(DtoValido(), empresaId);

        resultado.Id.Should().Be(idCreado);
        resultado.Simbolo.Should().Be("lb");
        _repo.Verify(r => r.CreateAsync(It.Is<UnidadMedida>(u =>
            u.Id == Guid.Empty && u.EmpresaId == empresaId && u.Simbolo == "lb")), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(
            "unidades_medida", AccionAuditoria.CREATE, empresaId,
            null, "UnidadMedida", idCreado, It.IsAny<object>(), null, null), Times.Once);
    }

    // ── DeactivateAsync ────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_NoExiste_LanzaNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((UnidadMedida?)null);

        var act = async () => await _service.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoTieneReferencias_LanzaBusinessException()
    {
        var unidad = Unidad("kg", TipoMedida.Peso, 1m);
        _repo.Setup(r => r.GetByIdAsync(unidad.Id, unidad.EmpresaId))
            .ReturnsAsync(unidad);
        _repo.Setup(r => r.ContarReferenciasAsync(unidad.Id, unidad.EmpresaId))
            .ReturnsAsync(3);

        var act = async () => await _service.DeactivateAsync(unidad.Id, unidad.EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Code.Should().Be("UNIDAD_EN_USO");
        ex.Which.Message.Should().Contain("3 tipo(s) de mercancía");
        _repo.Verify(r => r.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _auditoria.Verify(a => a.RegistrarAsync(
            "unidades_medida", AccionAuditoria.DEACTIVATE, It.IsAny<Guid>(),
            null, "UnidadMedida", It.IsAny<Guid>(), It.IsAny<object>(), null, null), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_SinReferencias_DesactivaCorrectamente()
    {
        var unidad = Unidad("kg", TipoMedida.Peso, 1m);
        _repo.Setup(r => r.GetByIdAsync(unidad.Id, unidad.EmpresaId))
            .ReturnsAsync(unidad);
        _repo.Setup(r => r.ContarReferenciasAsync(unidad.Id, unidad.EmpresaId))
            .ReturnsAsync(0);
        _repo.Setup(r => r.DeactivateAsync(unidad.Id, unidad.EmpresaId))
            .ReturnsAsync(true);

        var resultado = await _service.DeactivateAsync(unidad.Id, unidad.EmpresaId);

        resultado.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync(
            "unidades_medida", AccionAuditoria.DEACTIVATE, unidad.EmpresaId,
            null, "UnidadMedida", unidad.Id, It.IsAny<object>(), null, null), Times.Once);
    }
}
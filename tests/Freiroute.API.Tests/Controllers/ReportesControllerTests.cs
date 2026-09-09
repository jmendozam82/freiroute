using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.DTO.Orden;
using Freiroute.DTO.Reclamo;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración de reportes operacionales (HU-031 CA-07 · HU-032 CA-09) — Sprint 5.
/// Ambos endpoints exigen permiso analytics:read (módulo Analytics, ADR-006).
/// </summary>
[Collection("API Tests")]
public class ReportesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    /// <summary>Token con SOLO permisos de órdenes → sin analytics:read → 403.</summary>
    private readonly string _tokenSinAnalytics = JwtTestHelper.TokenOrdenes;

    /// <summary>Token de ADMIN con analytics:read → 200.</summary>
    private readonly string _tokenAnalytics;

    public ReportesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _tokenAnalytics = JwtTestHelper.GenerateTestToken(
            Guid.NewGuid(), JwtTestHelper.EmpresaTenant, new[] { "analytics:read" }, "ADMIN");
    }

    // ── GET /api/reportes/sla ─────────────────────────────────────

    [Fact]
    public async Task Sla_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var response = await client.GetAsync("/api/reportes/sla");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sla_ConTokenSinPermisoAnalytics_Retorna403()
    {
        // El claim "ordenes:read" NO satisface RequirePermission(analytics, READ).
        var client = _factory.CrearClientConToken(_tokenSinAnalytics);
        var response = await client.GetAsync("/api/reportes/sla");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Sla_ConPermisoAnalytics_Retorna200()
    {
        var client = _factory.CrearClientConToken(_tokenAnalytics);

        _factory.SlaService
            .Setup(s => s.GetReporteSlaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SlaReporteItemDto>
            {
                new SlaReporteItemDto
                {
                    ClienteId = Guid.NewGuid(),
                    ClienteNombre = "Distribuidora ABC S.A.",
                    TipoCliente = "VIP",
                    PorcentajeCumplimiento = 80.00m,
                    TotalOrdenes = 10,
                    OrdenesATiempo = 8,
                    OrdenesTardias = 2
                }
            });

        var response = await client.GetAsync("/api/reportes/sla");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<SlaReporteItemDto>>>();
        result!.Data!.Should().HaveCount(1);
        result.Data.Should().OnlyContain(i => i.PorcentajeCumplimiento == 80.00m);
    }

    // ── GET /api/reportes/reclamos ────────────────────────────────

    [Fact]
    public async Task Reclamos_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var response = await client.GetAsync("/api/reportes/reclamos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reclamos_ConTokenSinPermisoAnalytics_Retorna403()
    {
        var client = _factory.CrearClientConToken(_tokenSinAnalytics);
        var response = await client.GetAsync("/api/reportes/reclamos");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reclamos_ConPermisoAnalytics_Retorna200()
    {
        var client = _factory.CrearClientConToken(_tokenAnalytics);

        _factory.ReclamoService
            .Setup(s => s.GetReporteAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ReclamoReporteItemDto>
            {
                new ReclamoReporteItemDto { Tipo = TipoReclamo.Dano, Estado = EstadoReclamo.Abierto, Total = 3, MontoTotal = 4500m, MontoPromedio = 1500m }
            });

        var response = await client.GetAsync("/api/reportes/reclamos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ReclamoReporteItemDto>>>();
        result!.Data!.Should().HaveCount(1);
        result.Data.Should().OnlyContain(i => i.Tipo == TipoReclamo.Dano);
    }
}
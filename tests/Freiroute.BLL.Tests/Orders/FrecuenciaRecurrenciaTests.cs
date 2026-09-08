using System;
using FluentAssertions;
using Freiroute.Utility.Constants;
using Xunit;

namespace Freiroute.BLL.Tests.Orders;

public class FrecuenciaRecurrenciaTests
{
    private static readonly DateOnly Referencia = DateOnly.FromDateTime(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));

    [Theory]
    [InlineData("DIARIA", 1)]
    [InlineData("SEMANAL", 7)]
    [InlineData("QUINCENAL", 15)]
    public void CalcularProximaEjecucion_SumaDiasSegunFrecuencia(string frecuencia, int dias)
    {
        var proxima = FrecuenciaRecurrencia.CalcularProximaEjecucion(frecuencia, Referencia);

        proxima.Should().Be(Referencia.AddDays(dias));
    }

    [Fact]
    public void CalcularProximaEjecucion_Mensual_SumaUnMes()
    {
        var proxima = FrecuenciaRecurrencia.CalcularProximaEjecucion("MENSUAL", Referencia);

        proxima.Should().Be(Referencia.AddMonths(1));
    }

    [Fact]
    public void CalcularProximaEjecucion_MensualDia31_ManejaFinDeMes()
    {
        // 31 Ene + 1 mes → 28 Feb (2026 no es bisiesto). AddMonths aplica la
        // regla de "clamping" de .NET al final del mes.
        var finDeMes = new DateOnly(2026, 1, 31);
        var proxima = FrecuenciaRecurrencia.CalcularProximaEjecucion("MENSUAL", finDeMes);

        proxima.Should().Be(finDeMes.AddMonths(1));
    }

    [Fact]
    public void CalcularProximaEjecucion_FrecuenciaMinusculas_NormalizaMayusculas()
    {
        // El service hace ToUpper antes de llamar (ConfigurarRecurrenciaAsync),
        // pero el helper también tolera minúsculas.
        var proxima = FrecuenciaRecurrencia.CalcularProximaEjecucion("semanal", Referencia);

        proxima.Should().Be(Referencia.AddDays(7));
    }

    [Fact]
    public void CalcularProximaEjecucion_FrecuenciaDesconocida_DebeAplicarDefaultDiario()
    {
        // Comportamiento documentado: frecuencia no reconocida → +1 día (default).
        var proxima = FrecuenciaRecurrencia.CalcularProximaEjecucion("ANUAL", Referencia);

        proxima.Should().Be(Referencia.AddDays(1));
    }

    [Fact]
    public void GetLabel_DevuelveEtiquetaEnEspanol()
    {
        FrecuenciaRecurrencia.GetLabel("DIARIA").Should().Be("Diaria");
        FrecuenciaRecurrencia.GetLabel("SEMANAL").Should().Be("Semanal");
        FrecuenciaRecurrencia.GetLabel("QUINCENAL").Should().Be("Quincenal");
        FrecuenciaRecurrencia.GetLabel("MENSUAL").Should().Be("Mensual");
    }

    [Fact]
    public void GetLabel_FrecuenciaDesconocida_DevuelveLaMismaCadena()
    {
        FrecuenciaRecurrencia.GetLabel("ANUAL").Should().Be("ANUAL");
    }

    [Fact]
    public void Todos_ContieneLasCuatroFrecuencias()
    {
        FrecuenciaRecurrencia.Todos.Should().BeEquivalentTo(
            new[] { "DIARIA", "SEMANAL", "QUINCENAL", "MENSUAL" });
    }
}
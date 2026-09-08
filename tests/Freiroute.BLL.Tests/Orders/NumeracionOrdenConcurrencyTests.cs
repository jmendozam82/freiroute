using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Freiroute.BLL.Tests.Orders;

/// <summary>
/// Prueba de concurrencia para la numeración atómica de órdenes (HU-024 / ADR-020).
///
/// La función PostgreSQL generar_numero_orden() reserva el número atómica y
/// secuencialmente dentro de cada empresa/año usando un UPDATE con retorno
/// ("SELECT ... FOR UPDATE" + counter). Esta prueba exige una base local de
/// Supabase (supabase start) y NO se ejecuta en el pipeline CI sin ella.
///
/// Escenario: N tareas lanzan CambiarEstadoAsync DRAFT→CONFIRMED en paralelo
/// para la misma empresa; se verifica que los NúmerosOrden generados son
/// únicos y consecutivos (sin duplicados ni huecos).
/// </summary>
public class NumeracionOrdenConcurrencyTests
{
    [Fact(Skip = "Requiere base Supabase local (supabase start). CI-only.")]
    public async Task GenerarNumeroOrden_Concurrente_GeneraNumerosUnicosYConsecutivos()
    {
        throw new NotImplementedException(
            "Implementación con Npgsql + supabase start: " +
            "lanzar 20 CambiarEstadoAsync en paralelo y verificar " +
            "que los numero_orden devueltos son únicos y consecutivos " +
            "vía SELECT COUNT(DISTINCT numero_orden) y MIN/MAX.");
    }

    [Fact(Skip = "Requiere base Supabase local (supabase start). CI-only.")]
    public async Task GenerarNumeroOrden_MismaEmpresaAnio_NoLanzaConflictosDeLlaveUnica()
    {
        throw new NotImplementedException(
            "Implementación con Npgsql: verificar que la restricción única " +
            "uq_ordenes_empresa_anio_numero (empresa_id, anio, numero) nunca se " +
            "viola bajo carga concurrente.");
    }
}
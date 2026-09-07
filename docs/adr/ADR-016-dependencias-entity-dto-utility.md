# ADR-016: Enmienda a la Matriz de Dependencias — Entity y DTO pueden Referenciar Utility

## Estado
Aceptado

## Fecha
2026 — Sprint 3

## Contexto
ADR-002 definió la matriz de dependencias N-Tier: **Entity no referencia ningún proyecto** y **DTO solo referencia Entity**. Sprint 3 introduce catálogos de constantes de dominio (TipoUbicacion, ModoTransporte, TipoCliente, EstadoCredito, TipoTarifa, etc.) ubicadas en `Freiroute.Utility/Constants/` (según AGENTS.md).

Las Entities y los DTOs de los módulos de maestros necesitan usar esas constantes como valores por defecto y como defaults de validación de contrato (ej: `Tipo = TipoUbicacion.Otro`, `ModoTransporte = ModoTransporte.Ftl`). La alternativa — duplicar literales string en cada capa — viola DRY y crea riesgo de desincronización entre capas.

Sin la enmienda, Entity y DTO tendrían que hardcodear literales ("OTRO", "FTL", "REGULAR") que luego también existen en Utility, generando dos fuentes de verdad para el mismo dominio.

## Decisión
Ampliar la matriz de ADR-002 para permitir:

| Proyecto | Puede referenciar (nueva regla) |
|---|---|
| Entity | **Utility** (constantes de dominio) |
| DTO | Entity, **Utility** (constantes de dominio) |

Esta enmienda es segura porque:
1. **Utility no referencia ningún proyecto** — es la capa hoja de la solución. Agregar referencias hacia ella NO crea ciclos de dependencia.
2. Las constantes de dominio (tipo de ubicación, modo de transporte, estado de crédito) son parte del lenguaje ubicuo del sistema y deben ser compartidas por contrato entre capas.

## Implementación

```xml
<!-- Freiroute.Entity.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Freiroute.Utility\Freiroute.Utility.csproj" />
</ItemGroup>

<!-- Freiroute.DTO.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Freiroute.Entity\Freiroute.Entity.csproj" />
  <ProjectReference Include="..\Freiroute.Utility\Freiroute.Utility.csproj" />
</ItemGroup>
```

Uso en Entity (defaults de propiedad):
```csharp
public string Tipo { get; set; } = TipoUbicacion.Otro;
public string TipoCliente { get; set; } = TipoCliente.Regular;
public string EstadoCredito { get; set; } = EstadoCredito.AlDia;
```

Uso en DTO (defaults de contrato):
```csharp
public string ModoTransporte { get; set; } = ModoTransporte.Ftl;
public string TipoTarifa { get; set; } = TipoTarifa.FijoViaje;
```

## Alternativas Consideradas

1. **Literales string en Entity/DTO con constantes solo en Utility** — Descartada: dos fuentes de verdad para el mismo dominio; un cambio de valor requeriría editar múltiples capas.
2. **Mover las constantes de dominio a un proyecto nuevo** — Descartada: agrega un proyecto más a la solución sin beneficio real; Utility ya es la capa de constantes según AGENTS.md.
3. **Mantener ADR-002 intacto y usar enums C#** — Descartada: el sistema persiste strings en PostgreSQL (convención del proyecto) y los enums complican la conversión con Dapper.

## Consecuencias

**Positivas:**
- Fuente única de verdad para los catálogos de dominio (constantes C# ↔ valores SQL vía seeds)
- DTOs y Entities expresan el contrato con nombres de dominio, no literales mágicos
- Sin ciclos de dependencia: Utility sigue siendo capa hoja

**Negativas / Trade-offs:**
- Entity pierde pureza de "capa sin dependencias" — el costo es mínimo porque Utility es la capa más baja y no depende de nada
- ADR-002 queda enmendado: la matriz original aplica al resto de las referencias sin cambio

## Módulos Afectados
- EP-03 (Sprint 3): Entities y DTOs de Ubicaciones, Zonas, Mercancías, Unidades, Clientes y Tarifas
- Sprints futuros: cualquier Entity/DTO que necesite constantes de dominio de Utility
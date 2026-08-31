# ADR-006: UUID como Clave Primaria en lugar de INT/BIGINT

| Campo | Valor |
|---|---|
| **ID** | ADR-006 |
| **Título** | UUID (`gen_random_uuid()`) como clave primaria en lugar de INT/BIGINT autoincrementales |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-20 |
| **Decidido por** | Arquitecto de software + DBA |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

La elección del tipo de clave primaria afecta: seguridad, escalabilidad, rendimiento de índices, y facilidad de integración con sistemas externos.

En un sistema SaaS multi-tenant con múltiples clínicas/organizaciones, los IDs pueden ser expuestos en URLs, APIs y logs. Un ID predecible (`id=123`, `id=124`) expone información sobre el volumen de datos y permite ataques de enumeración (IDOR — Insecure Direct Object Reference).

---

## Decisión

**Todos los IDs serán UUID v4** generados por PostgreSQL con `gen_random_uuid()`.

```sql
id UUID PRIMARY KEY DEFAULT gen_random_uuid()
```

Los UUID se generan en la base de datos, no en el código de aplicación.

---

## Alternativas Evaluadas

### Opción A: SERIAL / BIGSERIAL (autoincremental) (RECHAZADA)

```sql
id SERIAL PRIMARY KEY          -- INT (hasta ~2.1 mil millones)
id BIGSERIAL PRIMARY KEY       -- BIGINT (hasta ~9.2 quintillones)
```

**Ventajas de INT/BIGSERIAL:**
- Mejor rendimiento en índices B-tree (enteros secuenciales vs. UUID aleatorio)
- JOINs más rápidos (enteros)
- El ID transmite información sobre el volumen: "registro #5432 de este cliente"
- URLs más amigables: `/pacientes/1234` vs. `/pacientes/550e8400-e29b-41d4-a716-446655440000`

**Desventajas que motivaron su rechazo:**
- **Ataque IDOR**: si la URL es `/api/pacientes/1234`, probar `1235`, `1236` puede exponer datos de otro paciente (aunque RLS mitiga esto, no es buena práctica)
- **Enumeración**: un competidor puede estimar el número de registros del sistema (`id=98723` implica ~100k registros)
- **Multi-tenant**: en un sistema multi-tenant, dos tenants podrían tener `paciente_id=1` — al hacer merge o integración, los IDs colisionan
- **Distribución**: si en el futuro se distribuye la BD, la generación de IDs secuenciales requiere coordinación central
- **Integración BaaS**: Supabase Auth genera UUIDs; mezclar tipos de ID (UUID para usuarios, INT para registros) complica las referencias

### Opción B: UUID v4 generado en la aplicación (RECHAZADA como forma primaria)

```csharp
// En el código C#
var id = Guid.NewGuid();
```

**Ventajas:**
- El código conoce el ID antes de insertar (útil para respuestas inmediatas)

**Desventajas que motivaron que la BD sea la fuente:**
- Requiere pasar el ID en cada INSERT — más código y posibilidad de colisiones (extremadamente rara pero posible)
- En PostgreSQL, `gen_random_uuid()` es más eficiente (función nativa)
- La BD debe ser la fuente de verdad del ID — no el código

### Opción C: ULID (Universally Unique Lexicographically Sortable ID) (EVALUADA, NO ELEGIDA)

**Ventajas de ULID:**
- UUID aleatorio + timestamp → lexicográficamente sortable
- Mejor rendimiento en índices que UUID v4 (secuencial en el tiempo)
- 26 caracteres vs. 36 de UUID

**Por qué no se eligió:**
- PostgreSQL no tiene soporte nativo para ULID (requiere extensión)
- Supabase Auth usa UUID — mezclar tipos agrega complejidad
- La ganancia de rendimiento en índices es observable solo en tablas con > 10 millones de registros
- Para el tamaño esperado del sistema, el costo es innecesario

### Opción D: UUID v4 generado por PostgreSQL (ELEGIDA) ✅

```sql
id UUID PRIMARY KEY DEFAULT gen_random_uuid()
```

**Ventajas:**
- Impredecibles — previenen ataques de enumeración (IDOR)
- Únicos globalmente — sin posibilidad de colisión entre tenants, sistemas o entornos
- Compatibles con Supabase Auth (que también genera UUIDs)
- No revelan información sobre el volumen de datos del sistema
- Permiten generar IDs offline o en sistemas distribuidos sin coordinación central
- PostgreSQL 13+ incluye `gen_random_uuid()` como función nativa (sin extensión)

**Desventajas aceptadas:**
- Mayor tamaño en índices (128 bits vs. 64 bits de BIGINT)
- Los UUID aleatorios causan fragmentación de páginas en índices B-tree (page splits) en tablas con muchos INSERTs
- Las URLs con UUID son menos estéticas (se puede resolver con slugs para URLs públicas)
- El rendimiento de índice es ~10-15% inferior a BIGSERIAL en tablas con > 1M filas (aceptable para el tamaño esperado)

---

## Consecuencias

### Positivas
- Todos los IDs son globalmente únicos — fusión de datos entre entornos o sistemas sin colisiones
- Sin ataque de enumeración en URLs de la API
- Compatibilidad directa con Supabase Auth (mismo tipo de dato)
- El código nunca "adivina" el próximo ID — siempre lo obtiene de la BD con `RETURNING id`

### Negativas / Trade-offs aceptados
- Para tablas con > 5 millones de filas y escrituras intensas, considerar UUID v7 (secuencial con timestamp) para mejor rendimiento de índices
- El tamaño de los índices es mayor vs. BIGINT
- Para URLs públicas y amigables, se puede agregar un campo `slug VARCHAR(100)` adicional sin cambiar el PK

### Patrón obligatorio en DAL (obtener el ID generado)

```csharp
// CORRECTO — obtener el UUID generado por PostgreSQL
const string sql = @"
    INSERT INTO pacientes (tenant_id, nombre, ...)
    VALUES (@TenantId, @Nombre, ...)
    RETURNING id";  -- PostgreSQL retorna el UUID generado

return await _db.ExecuteScalarAsync<Guid>(sql, paciente);
```

---

## Referencias

- [UUID vs. Integer Primary Keys](https://www.cybertec-postgresql.com/en/uuid-serial-or-identity-columns-for-postgresql-auto-generated-primary-keys/)
- [IDOR Attack — OWASP](https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/05-Authorization_Testing/04-Testing_for_Insecure_Direct_Object_References)
- [PostgreSQL gen_random_uuid()](https://www.postgresql.org/docs/current/functions-uuid.html)
- ADR-004 — RLS (UUID facilita el aislamiento por ser globalmente único)
- ADR-005 — Soft Delete (los UUIDs de registros inactivos no colisionan con nuevos registros)

# Spec: HU-001 — Registro de nuevo tenant

## Historia de Usuario
**Como** super administrador del sistema (Super Admin),  
**quiero** registrar una nueva empresa (tenant) en la plataforma,  
**para** activarla como tenant independiente con acceso aislado a sus propios datos.

> ⚠️ **Nota de Restricción:** Solo el rol `SUPER_ADMIN` puede ejecutar esta operación. Los administradores de tenant NO tienen permiso para crear otros tenants.

## Criterios de Aceptación
- [ ] **CA-01:** El sistema valida que `Nombre` sea obligatorio, único y mínimo 3 caracteres.
- [ ] **CA-02:** El sistema valida y asigna un `Slug` único válido para rutas web. Si se omite, se deriva automáticamente desde el nombre (lowercase, guiones, caracteres especiales removidos).
- [ ] **CA-03:** El sistema valida que el `Plan` sea exclusivamente uno de: `starter`, `professional`, `enterprise`.
- [ ] **CA-04:** Se registra `fecha_creacion = NOW()` automáticamente en la base de datos.
- [ ] **CA-05:** El estado inicial es `activo = true` por defecto.
- [ ] **CA-06:** Al crear el tenant, el sistema asigna automáticamente los permisos básicos predefinidos para el primer usuario ADMIN de esa empresa.

## Modelo de Dominio y Tabla
| Elemento | Detalle |
|---|---|
| Tabla SQL | `empresas` |
| Entidad C# | `Empresa` (`src/Freiroute.Entity/Empresa.cs`) |
| PK Generada | `gen_random_uuid()` en BD |
| Identificador Único | `slug` (VARCHAR(100) UNIQUE) + `id` (UUID) |
| Permisos Requeridos | `[RequirePermission("empresas", PermissionType.Create)]` (Solo SUPER_ADMIN) |

## Restricciones de Negocio TMS
1. Un slug NO puede coincidir con rutas reservadas del framework: `admin`, `login`, `api`, `static`, `auth`, `swagger`.
2. La creación de un tenant NO requiere ni acepta un `empresa_id` externo, ya que opera fuera del contexto multi-tenant.
3. El plan determina límites de recursos (usuarios activos, espacio de almacenamiento, módulos habilitados) aplicados en capas superiores.

## Contratos de Datos (DTOs)

### Request DTO: `EmpresaRequestDto`
```csharp
public class EmpresaRequestDto
{
    public string Nombre { get; set; }          // Requerido, min 3 chars
    public string Slug { get; set; } = "";      // Opcional. Auto-deriva si vacío
    public string Plan { get; set; }            // starter | professional | enterprise
}
```

### Response DTO: `EmpresaResponseDto`
```csharp
public class EmpresaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public string Slug { get; set; }
    public string Plan { get; set; }            // String representation
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
```

## Endpoints API Requeridos
| Método | Ruta | Permiso | Respuesta Éxito | Descripción |
|---|---|---|---|---|
| `POST` | `/api/empresas` | `SUPER_ADMIN` | `201 Created` | Crea nuevo tenant globalmente |

## Estrategia de Pruebas
| Tipo | Caso de Prueba | Expectativa |
|---|---|---|
| Unitario | `Crear_TenantConNombreValido_Returna201` | Persiste en BD, genera slug único |
| Unitario | `Crear_TenantSinNombre_LanzaValidationException` | Error 400 con mensaje en español |
| Unitario | `Crear_TenantConSlugInvalido_ReservaRutaWeb_LanzaError` | Bloquea slugs tipo `login` o `api` |
| Integración | `LoginComoTenantAdmin_IntentaCrearOtroTenant_Retorna403` | Deniega acceso por falta de permiso |
| Integración | `Crear_DuplicaSlug_Retorna409Conflict` | Valida unicidad antes de INSERT |

---

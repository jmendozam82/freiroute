# Requerimientos Funcionales, No Funcionales y Reglas de Negocio — Plantilla SaaS

> **Documento de requerimientos de referencia**
> Plantilla reutilizable para cualquier proyecto SaaS con el stack ASP.NET Core + Supabase.
> Los requerimientos marcados con ✅ son comunes a todo proyecto SaaS en este stack.
> Los marcados con 🔧 deben ser adaptados al dominio específico del negocio.

---

## 1. Requerimientos Funcionales

### RF-01: Autenticación y Sesión ✅

| ID | Descripción | Prioridad |
|---|---|---|
| RF-01.1 | El sistema permitirá a los usuarios iniciar sesión con email y contraseña | Alta |
| RF-01.2 | El sistema generará un JWT con `user_id`, `tenant_id`, `perfil_id` y `permisos[]` al autenticarse | Alta |
| RF-01.3 | El sistema cerrará la sesión automáticamente después de N horas de inactividad | Alta |
| RF-01.4 | El sistema permitirá recuperar la contraseña vía email | Media |
| RF-01.5 | El sistema mostrará un mensaje de error claro si las credenciales son incorrectas | Alta |
| RF-01.6 | El sistema redirigirá al usuario al módulo principal tras autenticarse correctamente | Alta |

### RF-02: Gestión de Perfiles ✅

| ID | Descripción | Prioridad |
|---|---|---|
| RF-02.1 | El sistema permitirá crear, editar y desactivar perfiles de usuario | Alta |
| RF-02.2 | Cada perfil tendrá un nombre, descripción y conjunto de permisos por módulo | Alta |
| RF-02.3 | Los permisos disponibles por módulo son: `READ`, `CREATE`, `UPDATE` | Alta |
| RF-02.4 | El perfil `ADMIN` tendrá acceso completo a todos los módulos de su tenant | Alta |
| RF-02.5 | No será posible eliminar un perfil que tenga usuarios asignados | Alta |

### RF-03: Gestión de Usuarios ✅

| ID | Descripción | Prioridad |
|---|---|---|
| RF-03.1 | El sistema permitirá crear, editar y desactivar usuarios | Alta |
| RF-03.2 | Cada usuario tendrá: nombre, apellido, email, teléfono, perfil asignado | Alta |
| RF-03.3 | El email será único por sistema (no por tenant) | Alta |
| RF-03.4 | Los usuarios desactivados no podrán iniciar sesión | Alta |
| RF-03.5 | El administrador podrá restablecer la contraseña de cualquier usuario de su tenant | Media |

### RF-04: Módulos de Catálogo 🔧

| ID | Descripción | Prioridad |
|---|---|---|
| RF-04.1 | El sistema permitirá gestionar los catálogos maestros del dominio de negocio | Alta |
| RF-04.2 | Cada catálogo tendrá operaciones: listar, crear, editar y desactivar | Alta |
| RF-04.3 | Los listados mostrarán solo registros activos por defecto | Alta |
| RF-04.4 | Los catálogos serán filtrados por `tenant_id` automáticamente | Alta |

### RF-05: Módulo Principal de Negocio 🔧

| ID | Descripción | Prioridad |
|---|---|---|
| RF-05.1 | [Describir la funcionalidad central del negocio según el dominio] | Alta |
| RF-05.2 | Los registros tendrán estados configurables según el flujo del negocio | Alta |
| RF-05.3 | El historial de cambios de estado será auditable | Media |

### RF-06: Reportes y Dashboard 🔧

| ID | Descripción | Prioridad |
|---|---|---|
| RF-06.1 | El sistema presentará un dashboard con indicadores clave del negocio (KPIs) | Media |
| RF-06.2 | Los reportes serán exportables en formato PDF y/o Excel | Media |
| RF-06.3 | Los datos del dashboard serán filtrados por `tenant_id` | Alta |
| RF-06.4 | Los reportes se filtrarán por rango de fechas | Media |

### RF-07: Alertas y Notificaciones ✅

| ID | Descripción | Prioridad |
|---|---|---|
| RF-07.1 | El sistema enviará notificaciones en tiempo real a los usuarios del mismo tenant | Media |
| RF-07.2 | Las alertas serán configurables por el administrador del tenant | Baja |
| RF-07.3 | Las notificaciones se mostrarán en la barra de navegación | Media |

### RF-08: Gestión de Archivos ✅

| ID | Descripción | Prioridad |
|---|---|---|
| RF-08.1 | El sistema permitirá subir archivos (PDF, imágenes) asociados a registros | Media |
| RF-08.2 | Los archivos se almacenarán en Supabase Storage con aislamiento por `tenant_id` | Alta |
| RF-08.3 | Las URLs de descarga serán temporales (tokens de acceso de duración limitada) | Alta |
| RF-08.4 | El tamaño máximo de archivo será de 10 MB por defecto (configurable) | Media |

### RF-09: API Pública (BaaS) ✅

| ID | Descripción | Prioridad |
|---|---|---|
| RF-09.1 | El sistema expondrá una API REST documentada con Swagger/OpenAPI | Alta |
| RF-09.2 | Los endpoints requerirán autenticación JWT | Alta |
| RF-09.3 | La API implementará paginación en todos los listados | Alta |
| RF-09.4 | Las respuestas seguirán el formato estándar `ApiResponse<T>` | Alta |

---

## 2. Requerimientos No Funcionales

### RNF-01: Rendimiento ✅

| ID | Descripción | Métrica |
|---|---|---|
| RNF-01.1 | El tiempo de respuesta del 95% de las peticiones será menor a 500ms | p95 < 500ms |
| RNF-01.2 | El sistema soportará al menos 100 usuarios concurrentes por tenant | 100 CCU/tenant |
| RNF-01.3 | Las páginas cargarán en menos de 3 segundos en conexión de 10 Mbps | < 3s TTI |
| RNF-01.4 | Los listados paginados retornarán máximo 20 registros por página | pageSize = 20 |

### RNF-02: Disponibilidad ✅

| ID | Descripción | Métrica |
|---|---|---|
| RNF-02.1 | El sistema tendrá una disponibilidad del 99.5% mensual | 99.5% uptime |
| RNF-02.2 | Las ventanas de mantenimiento serán notificadas con 48 horas de anticipación | 48h aviso |
| RNF-02.3 | El tiempo de recuperación ante fallos (RTO) será menor a 4 horas | RTO < 4h |
| RNF-02.4 | El punto objetivo de recuperación (RPO) será menor a 1 hora | RPO < 1h |

### RNF-03: Seguridad ✅

| ID | Descripción | Mecanismo |
|---|---|---|
| RNF-03.1 | Todas las comunicaciones usarán HTTPS/TLS 1.3 | TLS 1.3 |
| RNF-03.2 | Las contraseñas se almacenarán con hash bcrypt (Supabase Auth) | bcrypt |
| RNF-03.3 | Los tokens JWT expirarán en máximo 8 horas | exp < 8h |
| RNF-03.4 | El aislamiento de datos entre tenants será garantizado por RLS de PostgreSQL | RLS |
| RNF-03.5 | Los logs de acceso se conservarán por 90 días | 90 días |
| RNF-03.6 | Los intentos de login fallidos bloquearán la cuenta tras 5 intentos | 5 intentos |
| RNF-03.7 | Las variables de entorno sensibles nunca se commitearán al repositorio | .env |

### RNF-04: Escalabilidad ✅

| ID | Descripción | Mecanismo |
|---|---|---|
| RNF-04.1 | El sistema soportará un mínimo de 50 tenants sin degradación de rendimiento | Multi-tenant DB |
| RNF-04.2 | La arquitectura permitirá escalar horizontalmente el API sin cambios de código | Stateless API |
| RNF-04.3 | Supabase gestionará el auto-scaling de la base de datos | Supabase |
| RNF-04.4 | El storage de archivos no tendrá límite práctico (Supabase Storage) | S3-compatible |

### RNF-05: Mantenibilidad ✅

| ID | Descripción | Mecanismo |
|---|---|---|
| RNF-05.1 | La cobertura de tests unitarios en BLL será ≥ 80% | xUnit + Moq |
| RNF-05.2 | La cobertura de tests de integración en API será ≥ 60% | xUnit + TestServer |
| RNF-05.3 | Toda migración de BD pasará por Supabase CLI y se versionará en Git | Supabase CLI |
| RNF-05.4 | El código seguirá las convenciones definidas en AGENTS.md | AGENTS.md |
| RNF-05.5 | Cada módulo tendrá un `spec.md` antes de ser implementado | SDD |

### RNF-06: Usabilidad ✅

| ID | Descripción | Mecanismo |
|---|---|---|
| RNF-06.1 | La interfaz será responsiva y funcionará en dispositivos móviles | Bootstrap 5.3 |
| RNF-06.2 | Los mensajes de error serán claros y en el idioma del sistema | FluentValidation |
| RNF-06.3 | Todas las operaciones de escritura mostrarán confirmación de éxito o error | Toast/Alert |
| RNF-06.4 | Los formularios retendrán los datos ingresados si ocurre un error de validación | ModelState |
| RNF-06.5 | El sistema tendrá una navegación consistente en todos los módulos | Layout compartido |

### RNF-07: Compatibilidad ✅

| ID | Descripción | Alcance |
|---|---|---|
| RNF-07.1 | El sistema funcionará en los últimos 2 años de versiones de Chrome, Firefox, Edge | Navegadores |
| RNF-07.2 | El sistema funcionará en resoluciones desde 1280x720px | Desktop |
| RNF-07.3 | El sistema será accesible desde dispositivos móviles (768px+) | Mobile |
| RNF-07.4 | La API seguirá el estándar OpenAPI 3.0 | REST API |

### RNF-08: Trazabilidad y Auditoría ✅

| ID | Descripción | Mecanismo |
|---|---|---|
| RNF-08.1 | Todo registro tendrá `fecha_creacion` y `fecha_modificacion` | TIMESTAMPTZ |
| RNF-08.2 | Las operaciones críticas registrarán el `usuario_id` que las ejecutó | `creado_por` |
| RNF-08.3 | Los logs de errores del API serán estructurados (JSON) | Serilog |
| RNF-08.4 | El sistema registrará logs de inicio de sesión (exitosos y fallidos) | Supabase Auth |

---

## 3. Reglas de Negocio Universales (SaaS)

Las siguientes reglas aplican a **cualquier** proyecto SaaS construido con este stack:

### RN-U01: Aislamiento de Datos

> **Los datos de un tenant NUNCA serán visibles por otro tenant.**
> - Todo acceso a BD filtra por `tenant_id`.
> - RLS garantiza el aislamiento a nivel de base de datos.
> - Un bug en el código no puede violar el aislamiento de RLS.

### RN-U02: Soft Delete Universal

> **Ningún registro se elimina físicamente de la base de datos.**
> - Solo se desactiva (`activo = false`).
> - Los registros inactivos no aparecen en los listados por defecto.
> - El historial es permanente para auditoría.

### RN-U03: IDs Autogenerados

> **Los IDs son generados por la base de datos, no por el código.**
> - Tipo: `UUID` generado con `gen_random_uuid()`.
> - Ningún usuario puede especificar o predecir un ID.
> - Previene ataques de enumeración.

### RN-U04: Auditoría Mínima Obligatoria

> **Todo registro tiene trazabilidad temporal.**
> - `fecha_creacion`: TIMESTAMPTZ NOT NULL DEFAULT NOW()
> - `fecha_modificacion`: TIMESTAMPTZ (nullable, se actualiza en cada UPDATE)
> - Para operaciones críticas: `creado_por UUID` (referencia al usuario que creó el registro)

### RN-U05: Validación en Dos Capas

> **La validación se aplica siempre en el servidor.**
> - FluentValidation en BLL (authoritative).
> - jQuery Validate en el cliente (UX solamente).
> - Nunca confiar solo en la validación del cliente.

### RN-U06: Permisos Granulares

> **El acceso a cada operación se controla explícitamente.**
> - Tres permisos: `READ`, `CREATE`, `UPDATE`.
> - Sin permiso de `DELETE` (ver RN-U02).
> - El perfil `ADMIN` tiene todos los permisos de su tenant.
> - Cada endpoint del API verifica permisos antes de ejecutar.

### RN-U07: DTOs como Interfaz Pública

> **Las Entities nunca se exponen directamente al cliente.**
> - RequestDto: datos de entrada (crear/editar).
> - ResponseDto: datos de salida (lectura).
> - Permite cambiar la BD sin romper contratos de API.

### RN-U08: Paginación Universal

> **Los listados siempre se paginan.**
> - Tamaño de página por defecto: 20 registros.
> - Tamaño máximo de página: 100 registros.
> - Los endpoints de listado aceptan parámetros `page` y `pageSize`.

### RN-U09: Archivos en Storage Privado

> **Los archivos almacenados no tienen URLs públicas permanentes.**
> - Los buckets de Storage son privados.
> - Las URLs se generan con tokens de acceso de duración limitada.
> - Los paths incluyen `{tenant_id}` para garantizar aislamiento.

### RN-U10: Migraciones Versionadas

> **La base de datos evoluciona solo a través de migraciones.**
> - Toda modificación de esquema es una migración SQL versionada.
> - Las migraciones se commitean en Git junto con el código.
> - Prohibido ejecutar SQL ad-hoc en producción.

---

## 4. Reglas de Negocio Específicas del Dominio 🔧

> **Reglas críticas para el dominio TMS (Transport Management System).**
> **Estas reglas deben completarse antes de iniciar la implementación de cualquier módulo.**

### RN-D01: Envíos (Shipments) y Embarques

> **Todo embarque debe tener un ciclo de vida controlado y auditado.**
> - Los embarques se crean en estado `PLANNED` y evolucionan por estados definidos
> - Los estados válidos son: `PLANNED → CONFIRMED → ASSIGNED → PICKUP_SCHEDULED → IN_TRANSIT → DELIVERED → INVOICED → CLOSED`
> - Los embarques no se eliminan jamás; solo se desactivan (`activo = false`) manteniendo el historial
> - Cada cambio de estado debe registrar: usuario, timestamp y motivo opcional
> - Los embarques CLOSED no pueden ser modificados, solo consultados

### RN-D02: Asignación de Carriers y Múltiples Modos de Transporte

> **Los carriers y vehículos deben ser asignados respectando restricciones de negocio.**
> - Un carrier solo puede ser asignado a un embarque si su estado es `Activo` y no está bloqueado
> - La asignación debe considerar la zona geográfica de cobertura del carrier vs. la ruta del embarque
> - Los modos de transporte (FTL/LTL/Aéreo/Marítimo/Intermodal) son exclusivos por embarque
> - Un mismo embarque no puede tener múltiples carriers del mismo modo de transporte
> - Si un carrier queda bloqueado (documentos vencidos, blacklist), las asignaciones pendientes se reubican automáticamente

### RN-D03: Geocodificación y Validación de Direcciones

> **Todas las ubicaciones (origen/destino) deben validarse geográficamente.**
> - Las direcciones deben geocodificarse al crear ubicaciones (clientes, almacenes, puertos)
> - Las coordenadas (lat/lng) son obligatorias en las tablas de ubicaciones
> - No se pueden crear embarques con ubicaciones no geocodificadas
> - El sistema sugiere correcciones cuando la geocodificación falla o devuelve múltiples resultados
> - Las validaciones de cobertura de carrier deben aplicarse sobre las coordenadas registradas

### RN-D04: Cálculo de Costos y Múltiples Monedas

> **El costo de flete se calcula automáticamente y debe ser trazable.**
> - El costo base se calcula por: tarifa contractual × distancia + recargos (combustible, peaje, seguro)
> - Las conversiones de moneda usan la tasa del día (fuente externa o configurada por tenant)
> - Los desgloses de costo deben guardarse en el registro del embarque (costo estimado vs. costo real vs. costo facturado)
> - Los descuentos y penalizaciones se aplican según términos de contrato y se auditan
> - Los reportes financieros deben filtro por moneda y conversión automática

### RN-D05: Track & Trace y Eventos de Trayecto

> **Cada embarque debe tener trazabilidad de eventos en tiempo real.**
> - Los eventos GPS automáticos: salida de origen, llegada a parada, salida de parada, llegada a destino
> - Los eventos manuales (firmas, incidentes, averías) requieren validación de usuario y timestamp
> - Los geofences alrededor de ubicaciones clave generan alertas automáticas de entrada/salida
> - El ETA se recalcula cada 5 minutos considerando tráfico y condiciones de ruta
> - Los historial de eventos es immutable (nunca se modifica), solo se pueden agregar nuevos eventos

### RN-D06: Documentos y Facturación de Flete

> **Todos los documentos críticos generan registro y auditoría.**
> - Carta de Porte/Bill of Lading se genera automáticamente al confirmar embarque
> - POD (Proof of Delivery) digital debe capturarse (firma, foto, GPS) al momento de la entrega
> - Facturas a clientes se generan al cerrar embarque y no pueden ser eliminadas, solo anuladas
> - Los documentos firmados son bloqueados para edición (solo consulta)
> - Los comprobantes fiscales (CFDI, etc.) deben relacionarse con el ID del embarque
> - Los récords de documentos incluyen: tipo, número, fecha emisión, version, usuario que generó, tenant_id

### RN-D07: Estados de Excepción y Re-clasificación

> **Los embarques pueden desviarse de la ruta planeada y requieren manejo de excepciones.**
> - Estados de excepción: `CANCELLED`, `ON_HOLD`, `FAILED_DELIVERY`, `PARTIALLY_SPLIT`
> - Un embarque en `FAILED_DELIVERY` requiere: motivo, nuevo intento programado, cargo adicional calculado
> - Los embarques `PARTIALLY_SPLIT` deben tener sus splits vinculados y el original en estado híbrido
> - Cuando un embarque se re-planifica en tiempo real, se registra la decisión del dispatcher y el motivo
> - Los embarques `ON_HOLD` no consumen capacidad de vehicle y pueden ser reasignados

### RN-D08: Límite de Carga y Restricciones de Vehículo

> **La capacidad del vehículo debe respetarse en todas las operaciones.**
> - Cada vehículo tiene capacidad máxima por peso, volumen y tipo de mercancía
> - El sistema debe validar antes de asignar: peso total, volumen total, restricciones de tipo (peligroso, frágil, temperatura controlada)
> - Las consolidaciones de órdenes deben verificar compatibilidad antes de crear el shipment agrupado
> - Si se excede la capacidad, el sistema sugiere: división del embarque, vehículo adicional o re-priorización
> - Los warnings de capacidad excesiva deben registrarse en auditoría aunque la operación sea permitida

### RN-D09: Permisos y Roles Específicos TMS

> **El control de acceso debe ser granular por operación TMS.**
> - Los 3 permisos básicos: `READ`, `CREATE`, `UPDATE` se aplican por módulo TMS
> - Módulos específicos con permisos adicionales: `EXPORT`, `APPROVE`, `CONSOLIDATE`, `SPLIT`
> - El perfil `ADMIN` de tenant tiene acceso a todos los módulos de su tenant (no global)
> - Los permisos de `DELETE` nunca existen (usar soft delete RN-D01)
> - Cada endpoint API debe verificar permisos antes de ejecutar operación crítica

### RN-D10: Retención y Conformidad Documental

> **Los documentos y registros deben ser retenidos peródicos según regulación.**
> - Documentos de embarque (Carta de Porte, POD, Manifest): mínimo 5 años
> - Registros de auditoría de estados: mínimo 3 años
> - Registros financieros y facturas: mínimo 10 años (o según regulación local)
> - Logs de acceso y eventos GPS: mínimo 2 años
> - Los backups de storage deben retenerse según política de disaster recovery del tenant
> - Los records immutables no pueden ser modificados, solo consultados o exportados

---

## 5. Glosario del Dominio 🔧

> Completar con los términos específicos del negocio en cada proyecto.

| Término | Definición |
|---|---|
| **Tenant** | Organización/empresa que contrata el servicio SaaS |
| **[Entidad central]** | [Descripción de la entidad principal del negocio] |
| **[Estado 1]** | [Descripción del estado] |
| **[Estado 2]** | [Descripción del estado] |

---

*requerimientos.md — Plantilla de requerimientos para proyectos SaaS*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*

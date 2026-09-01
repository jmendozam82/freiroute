# Product Backlog — Plantilla SaaS con SCRUM

> **Backlog de referencia para proyectos SaaS**
> Contiene las Historias de Usuario universales organizadas por Sprint.
> Las HUs marcadas con ✅ son comunes a todo proyecto SaaS en este stack.
> Las marcadas con 🔧 deben ser adaptadas al dominio específico del negocio.

---

## Definición de Story Points

| Story Points | Complejidad | Tiempo estimado (con agentes IA) |
|---|---|---|
| 1 | Trivial | < 2 horas |
| 2 | Simple | 2-4 horas |
| 3 | Moderada | 4-8 horas (1 día) |
| 5 | Compleja | 2-3 días |
| 8 | Muy compleja | 4-5 días |
| 13 | Épica (dividir) | > 1 semana |

---

## Sprint 0 — Fundamentos del Proyecto (2 semanas)

> **Objetivo:** Infraestructura lista para comenzar el desarrollo.
> **Entregable:** Proyecto corriendo localmente con autenticación básica.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-00.1 | **Como** DevOps Lead, **quiero** inicializar el repositorio con la estructura estándar **para** que todos los agentes tengan el contexto correcto desde el inicio | 2 | ✅ | - AGENTS.md commiteado primero - 8 proyectos .NET creados - Supabase inicializado - GitHub Actions básico |
| HU-00.2 | **Como** DBA, **quiero** crear el schema inicial de la BD **para** establecer las tablas base del sistema | 3 | ✅ | - Tabla `tenants` creada - Tabla `perfiles` creada - Tabla `usuarios` creada - Tabla `permisos` creada - RLS habilitado en todas |
| HU-00.3 | **Como** Arquitecto, **quiero** configurar la inyección de dependencias base **para** que todos los módulos posteriores sigan el mismo patrón | 2 | ✅ | - IOC configurado - ApiResponse<T> implementado - Middleware de tenant configurado - JWT configurado |
| HU-00.4 | **Como** equipo, **quiero** configurar el pipeline de CI/CD **para** que los tests corran automáticamente en cada PR | 2 | ✅ | - GitHub Actions ejecuta tests en cada PR - Deploy automático a staging en push a develop |

**Velocidad Sprint 0:** 9 Story Points

---

## Sprint 1 — Autenticación y Usuarios (2 semanas)

> **Objetivo:** Sistema de acceso seguro y gestión básica de usuarios.
> **Entregable:** Login funcional, perfiles y usuarios CRUD completo.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-01.1 | **Como** usuario, **quiero** iniciar sesión con email y contraseña **para** acceder al sistema de forma segura | 5 | ✅ | - Formulario de login - JWT generado con tenant_id, perfil_id, permisos[] - Redirección post-login - Mensaje de error si credenciales incorrectas |
| HU-01.2 | **Como** usuario, **quiero** cerrar sesión **para** proteger mi cuenta cuando termine mi turno | 1 | ✅ | - Botón de logout en navbar - JWT invalidado - Redirección a login |
| HU-01.3 | **Como** administrador, **quiero** gestionar los perfiles de usuario **para** controlar qué puede hacer cada tipo de usuario | 5 | ✅ | - CRUD de perfiles - Asignación de permisos READ/CREATE/UPDATE por módulo - Soft delete (desactivar) |
| HU-01.4 | **Como** administrador, **quiero** gestionar los usuarios **para** controlar quién tiene acceso al sistema | 5 | ✅ | - CRUD de usuarios - Asignación de perfil - Soft delete - No eliminar si tiene registros asociados |
| HU-01.5 | **Como** usuario, **quiero** recuperar mi contraseña olvidada **para** no perder acceso al sistema | 3 | ✅ | - Formulario de recuperación - Email enviado con link temporal - Link expira en 24h |

**Velocidad Sprint 1:** 19 Story Points

**Tests requeridos Sprint 1:**
- Unit tests: `AuthServiceTests`, `PerfilServiceTests`, `UsuarioServiceTests`
- Integration tests: `AuthControllerTests`, `PerfilesControllerTests`, `UsuariosControllerTests`

---

## Sprint 2 — Catálogos Maestros (2 semanas)

> **Objetivo:** Catálogos del dominio de negocio listos para ser usados.
> **Entregable:** Todos los catálogos maestros CRUD con permisos.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-02.1 | **Como** administrador, **quiero** gestionar [Catálogo 1] **para** tener la información base del sistema | 3 | 🔧 | - CRUD completo - Filtro por tenant_id automático - Soft delete - Validación FluentValidation |
| HU-02.2 | **Como** administrador, **quiero** gestionar [Catálogo 2] **para** [objetivo del catálogo] | 3 | 🔧 | - CRUD completo - Verificación de dependencias antes de desactivar |
| HU-02.3 | **Como** administrador, **quiero** gestionar [Catálogo 3] **para** [objetivo del catálogo] | 3 | 🔧 | - CRUD completo |
| HU-02.4 | **Como** administrador, **quiero** gestionar [Catálogo 4] **para** [objetivo del catálogo] | 3 | 🔧 | - CRUD completo |
| HU-02.5 | **Como** administrador, **quiero** importar datos iniciales desde un archivo CSV **para** agilizar la configuración inicial | 5 | 🔧 | - Subida de CSV - Validación de formato - Importación por lotes - Reporte de errores |

**Velocidad Sprint 2:** 17 Story Points

---

## Sprint 3 — Módulo Principal de Negocio (2 semanas)

> **Objetivo:** La funcionalidad central del producto funcionando.
> **Entregable:** Módulo principal CRUD con flujo de estados.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-03.1 | **Como** [rol principal], **quiero** [crear/gestionar la entidad principal] **para** [objetivo del negocio] | 8 | 🔧 | - CRUD completo - Flujo de estados - Filtros por fecha, estado, etc. - Búsqueda por múltiples campos |
| HU-03.2 | **Como** [rol], **quiero** ver el detalle completo de [entidad] **para** [objetivo] | 5 | 🔧 | - Vista de detalle - Historial de cambios - Archivos adjuntos (si aplica) |
| HU-03.3 | **Como** [rol], **quiero** cambiar el estado de [entidad] **para** reflejar el avance del proceso | 3 | 🔧 | - Transiciones de estado válidas - Registro de cambio en auditoría - Notificación en tiempo real |
| HU-03.4 | **Como** administrador, **quiero** ver todas las [entidades] de mi organización **para** tener visibilidad global | 3 | 🔧 | - Vista admin con filtros - Paginación - Exportar a Excel |

**Velocidad Sprint 3:** 19 Story Points

---

## Sprint 4 — Funcionalidades Avanzadas (2 semanas)

> **Objetivo:** Características que diferencian el producto.
> **Entregable:** Tiempo real, archivos, búsquedas avanzadas.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-04.1 | **Como** usuario, **quiero** recibir notificaciones en tiempo real **para** estar informado de cambios importantes | 8 | ✅ | - Supabase Realtime + SignalR - Notificaciones en navbar - Sin recargar la página |
| HU-04.2 | **Como** usuario, **quiero** adjuntar archivos a los registros **para** tener documentación centralizada | 5 | ✅ | - Upload a Supabase Storage - Máximo 10 MB por archivo - PDF e imágenes soportados - URL temporal para descarga |
| HU-04.3 | **Como** usuario, **quiero** buscar registros por múltiples criterios **para** encontrar información rápidamente | 5 | 🔧 | - Búsqueda por texto libre - Filtros combinables - Resultados instantáneos |
| HU-04.4 | **Como** administrador, **quiero** configurar las alertas del sistema **para** adaptar las notificaciones al flujo de mi organización | 3 | ✅ | - Configuración por tenant - Tipos de alertas - Umbrales configurables |

**Velocidad Sprint 4:** 21 Story Points

---

## Sprint 5 — Dashboard y Reportes (2 semanas)

> **Objetivo:** Visibilidad de métricas clave del negocio.
> **Entregable:** Dashboard interactivo y reportes exportables.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-05.1 | **Como** gerente, **quiero** ver un dashboard con los KPIs de mi organización **para** tomar decisiones basadas en datos | 8 | 🔧 | - Gráficas interactivas - Filtro por rango de fechas - Datos en tiempo real - Tarjetas de resumen |
| HU-05.2 | **Como** gerente, **quiero** generar un reporte de [entidad principal] **para** analizar el desempeño del período | 5 | 🔧 | - Filtros: fechas, estados, [dimensión del negocio] - Exportar a PDF - Exportar a Excel |
| HU-05.3 | **Como** gerente, **quiero** ver reportes de actividad por usuario **para** evaluar la carga de trabajo del equipo | 3 | ✅ | - Registros creados por usuario - Rangos de tiempo - Exportable |
| HU-05.4 | **Como** administrador del sistema, **quiero** ver métricas de uso del sistema **para** planificar el crecimiento | 3 | ✅ | - Tenants activos - Usuarios activos - Volumen de datos - Tendencias |

**Velocidad Sprint 5:** 19 Story Points

---

## Sprint 6 — Landing Page y API Pública (2 semanas)

> **Objetivo:** Presencia pública del producto y API documentada.
> **Entregable:** Landing page y Swagger completo.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-06.1 | **Como** visitante, **quiero** ver una landing page informativa **para** entender el valor del producto antes de contratar | 8 | 🔧 | - Hero section - Características del producto - Planes y precios - Formulario de contacto - SEO básico |
| HU-06.2 | **Como** desarrollador externo, **quiero** acceder a la documentación de la API **para** integrar mis sistemas | 3 | ✅ | - Swagger UI accesible - Todos los endpoints documentados - Ejemplos de request/response - Autenticación explicada |
| HU-06.3 | **Como** administrador del sistema, **quiero** gestionar las organizaciones (tenants) **para** dar de alta nuevos clientes | 5 | ✅ | - CRUD de tenants (Super Admin) - Configuración por tenant - Plan de suscripción |
| HU-06.4 | **Como** cliente potencial, **quiero** solicitar una demo **para** evaluar el producto antes de comprar | 2 | 🔧 | - Formulario de solicitud - Email de confirmación - Notificación al equipo de ventas |

**Velocidad Sprint 6:** 18 Story Points

---

## Sprint 7 — Calidad, Seguridad y Performance (2 semanas)

> **Objetivo:** El sistema es seguro, rápido y bien testeado.
> **Entregable:** Cobertura de tests al objetivo, auditoría de seguridad.

| ID | Historia de Usuario | SP | Tipo | Criterios de Aceptación |
|---|---|---|---|---|
| HU-07.1 | **Como** equipo, **quiero** completar la cobertura de tests al objetivo **para** garantizar la calidad del sistema | 8 | ✅ | - BLL tests ≥ 80% - API tests ≥ 60% - Reporte de cobertura en CI |
| HU-07.2 | **Como** equipo, **quiero** realizar una auditoría de seguridad **para** identificar y corregir vulnerabilidades | 5 | ✅ | - RLS verificado en todas las tablas - Endpoints protegidos con permisos - Headers de seguridad configurados - Secrets en variables de entorno |
| HU-07.3 | **Como** equipo, **quiero** optimizar el rendimiento de las consultas lentas **para** cumplir los RNF de tiempo de respuesta | 5 | ✅ | - Índices de BD revisados - Consultas lentas (>200ms) optimizadas - Caché implementado donde aplica |
| HU-07.4 | **Como** equipo, **quiero** documentar los manuales de usuario **para** facilitar el onboarding de nuevos clientes | 3 | 🔧 | - Manual de usuario por rol - Guía de configuración inicial - Preguntas frecuentes |

**Velocidad Sprint 7:** 21 Story Points

---

## Backlog de Épicas Futuras (Post MVP)

| ID | Épica | Prioridad | Descripción |
|---|---|---|---|
| EP-01 | Integración con servicios externos | Media | Webhooks, Zapier, API de terceros |
| EP-02 | Aplicación móvil | Baja | App iOS/Android (React Native o MAUI) |
| EP-03 | Multiidioma | Baja | i18n para mercados internacionales |
| EP-04 | IA integrada | Media | Sugerencias automáticas, análisis predictivo |
| EP-05 | Self-service onboarding | Alta | Que los clientes se registren solos sin intervención |
| EP-06 | Facturación y pagos | Alta | Integración con Stripe o equivalente |
| EP-07 | SSO / OAuth | Media | Login con Google, Microsoft, etc. |
| EP-08 | Auditoría avanzada | Media | Historial completo de cambios por entidad |

---

## Resumen del Backlog

| Sprint | Objetivo | Story Points | Duración |
|---|---|---|---|
| Sprint 0 | Fundamentos del Proyecto | 9 SP | 2 semanas |
| Sprint 1 | Autenticación y Usuarios | 19 SP | 2 semanas |
| Sprint 2 | Catálogos Maestros | 17 SP | 2 semanas |
| Sprint 3 | Módulo Principal | 19 SP | 2 semanas |
| Sprint 4 | Funcionalidades Avanzadas | 21 SP | 2 semanas |
| Sprint 5 | Dashboard y Reportes | 19 SP | 2 semanas |
| Sprint 6 | Landing Page y API Pública | 18 SP | 2 semanas |
| Sprint 7 | Calidad y Seguridad | 21 SP | 2 semanas |
| **Total MVP** | **Sistema completo en producción** | **143 SP** | **16 semanas** |

> **Con agentes de IA trabajando en paralelo:** estimado real ~10-12 semanas.

---

## Plantilla de Historia de Usuario

```markdown
## HU-XX: [Título de la Historia]

**Como** [rol/usuario],
**quiero** [funcionalidad/acción],
**para** [beneficio/objetivo del negocio].

### Criterios de Aceptación

- [ ] [Criterio 1: comportamiento observable y verificable]
- [ ] [Criterio 2]
- [ ] [Criterio 3]

### Notas técnicas

- Migración requerida: [sí/no] — [nombre de la migración]
- Módulo afectado: [nombre del módulo]
- Permisos requeridos: [READ | CREATE | UPDATE]
- Tiempo real: [sí/no]
- Archivos: [sí/no]

### Story Points: [1 | 2 | 3 | 5 | 8 | 13]
### Prioridad: [Alta | Media | Baja]
### Sprint: [número]
```

---

*backlog.md — Plantilla de Product Backlog para proyectos SaaS con SCRUM*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*

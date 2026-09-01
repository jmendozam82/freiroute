# 🚛 FREIROUTE TMS — Product Backlog Completo
**Sistema:** Freiroute TMS SaaS Multi-Tenant  
**Versión:** 1.0  
**Referencia:** Oracle TMS · SAP TM · MercuryGate · BluJay · Trimble TMS  
**Metodología:** Scrum  
**Fecha:** 2026  

---

## 📌 Convenciones

| Código | Significado |
|---|---|
| `HU-XXX` | Historia de Usuario |
| `EP-XX` | Épica (módulo) |
| `SP-XX` | Sprint |
| **Alta** | Prioridad crítica para MVP |
| **Media** | Importante, segunda fase |
| **Baja** | Deseable, tercera fase |

**Formato HU:**
> **Como** [rol], **quiero** [acción], **para** [valor de negocio]  
> **Criterios de aceptación:** lista de condiciones verificables  
> **Estimación:** puntos de historia (Fibonacci)

---

## 🗺️ MAPA DE ÉPICAS

| Épica | Módulo | Sprints |
|---|---|---|
| EP-01 | Infraestructura Multi-Tenant & Auth | SP-01 |
| EP-02 | Administración SaaS & Tenants | SP-02 |
| EP-03 | Gestión de Maestros (Catálogos) | SP-03 |
| EP-04 | Order Management | SP-04 – SP-05 |
| EP-05 | Carrier Management | SP-06 |
| EP-06 | Shipment Planning | SP-07 – SP-08 |
| EP-07 | Route Optimization | SP-09 |
| EP-08 | Track & Trace | SP-10 |
| EP-09 | Document Management | SP-11 |
| EP-10 | Freight Audit & Payment | SP-12 – SP-13 |
| EP-11 | Customer Portal & CRM | SP-14 |
| EP-12 | Warehouse & Dock Management | SP-15 |
| EP-13 | Comercio Internacional & Aduanas | SP-16 |
| EP-14 | Fleet & Driver Management | SP-17 |
| EP-15 | Compliance & Safety | SP-18 |
| EP-16 | Analytics & Business Intelligence | SP-19 – SP-20 |
| EP-17 | Integraciones & API Pública | SP-21 – SP-22 |
| EP-18 | Mobile App — Conductor | SP-23 – SP-24 |
| EP-19 | Notificaciones & Alertas | SP-25 |
| EP-20 | Configuración & Localización | SP-26 |

**Total estimado: 26 Sprints · ~156 Historias de Usuario**

---

---

# 🔐 EP-01 — Infraestructura Multi-Tenant & Autenticación

## Sprint 1 — Fundación del Sistema

---

### HU-001 · Registro de nuevo tenant
**Como** administrador SaaS, **quiero** registrar una nueva empresa en la plataforma, **para** activarla como tenant independiente con su propio espacio de datos.

**Criterios de aceptación:**
- [ ] El sistema crea un tenant con ID único (UUID)
- [ ] Se aprovisiona un schema de base de datos aislado `tenant_{uuid}` en Supabase
- [ ] Se envía email de confirmación al administrador del tenant
- [ ] El tenant queda en estado `ACTIVE` por defecto
- [ ] Se registra fecha de creación y plan de suscripción

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-002 · Aislamiento de datos por tenant (Row Level Security)
**Como** arquitecto del sistema, **quiero** que cada tenant acceda únicamente a sus propios datos, **para** garantizar seguridad y privacidad total entre empresas.

**Criterios de aceptación:**
- [ ] Implementación de RLS (Row Level Security) en Supabase para todas las tablas
- [ ] Cada query incluye automáticamente el filtro `tenant_id` del usuario autenticado
- [ ] Pruebas de penetración confirman que un tenant A no puede leer datos del tenant B
- [ ] Los logs de acceso registran `tenant_id` en cada operación

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-003 · Registro e inicio de sesión de usuario
**Como** usuario del sistema, **quiero** registrarme e iniciar sesión con email y contraseña, **para** acceder a la plataforma de forma segura.

**Criterios de aceptación:**
- [ ] Registro con email, contraseña y nombre completo
- [ ] Validación de email único por tenant
- [ ] Contraseña con mínimo 8 caracteres, mayúscula, número y carácter especial
- [ ] Login con JWT válido por 8 horas
- [ ] Refresh token con vigencia de 30 días
- [ ] Bloqueo de cuenta tras 5 intentos fallidos

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-004 · Autenticación con OAuth 2.0 (Google / Microsoft)
**Como** usuario, **quiero** iniciar sesión con mi cuenta de Google o Microsoft, **para** acceder sin recordar otra contraseña.

**Criterios de aceptación:**
- [ ] Botón "Iniciar sesión con Google" funcional
- [ ] Botón "Iniciar sesión con Microsoft" funcional
- [ ] Si el email ya existe, se vincula la cuenta existente
- [ ] Si es nuevo usuario, se crea automáticamente en el tenant correspondiente
- [ ] El token SSO se mapea al JWT interno del sistema

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-005 · Autenticación de dos factores (2FA)
**Como** administrador, **quiero** habilitar 2FA para usuarios del sistema, **para** reforzar la seguridad de acceso.

**Criterios de aceptación:**
- [ ] Soporte para TOTP (Google Authenticator, Authy)
- [ ] Soporte para 2FA por email (código de 6 dígitos)
- [ ] El administrador puede hacer 2FA obligatorio por rol
- [ ] El usuario puede desactivar 2FA solo con verificación previa
- [ ] Códigos de recuperación de un solo uso disponibles

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-006 · Gestión de roles y permisos (RBAC)
**Como** administrador del tenant, **quiero** definir roles con permisos granulares por módulo, **para** controlar qué puede hacer cada usuario en el sistema.

**Criterios de aceptación:**
- [ ] Roles predeterminados: Super Admin, Admin, Dispatcher, Operador, Conductor, Cliente, Auditor
- [ ] Permisos por módulo: Ver, Crear, Editar, Eliminar, Aprobar, Exportar
- [ ] Posibilidad de crear roles personalizados
- [ ] Cambio de rol aplicado en tiempo real sin cerrar sesión
- [ ] Log de cambios de permisos con usuario y timestamp

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-007 · Recuperación de contraseña
**Como** usuario, **quiero** recuperar mi contraseña desde el email, **para** retomar acceso si la olvidé.

**Criterios de aceptación:**
- [ ] Formulario de recuperación por email
- [ ] Token de recuperación válido por 30 minutos
- [ ] Link de un solo uso
- [ ] Notificación al usuario si alguien solicita recuperación de su cuenta
- [ ] Redirige al login con mensaje de éxito tras restablecer

**Estimación:** 3 pts | **Prioridad:** Alta

---

### HU-008 · Auditoría de accesos y actividad
**Como** administrador, **quiero** ver un log de todas las acciones realizadas en el sistema, **para** auditar actividad y detectar comportamientos anómalos.

**Criterios de aceptación:**
- [ ] Registro de: login, logout, creación, edición, eliminación, exportación
- [ ] Log incluye: usuario, IP, tenant, módulo, acción, timestamp
- [ ] Filtros por usuario, módulo, rango de fechas y tipo de acción
- [ ] Exportación del log a CSV/Excel
- [ ] Retención mínima de 12 meses

**Estimación:** 5 pts | **Prioridad:** Media

---

---

# 🏢 EP-02 — Administración SaaS & Gestión de Tenants

## Sprint 2 — Panel SaaS

---

### HU-009 · Panel de administración global (Super Admin)
**Como** super administrador de Freiroute, **quiero** un panel central de gestión de todos los tenants, **para** monitorear la plataforma completa.

**Criterios de aceptación:**
- [ ] Lista de todos los tenants con estado, plan, fecha de registro
- [ ] Métricas globales: usuarios activos, embarques del día, storage usado
- [ ] Acceso de impersonación a cualquier tenant (con log de auditoría)
- [ ] Acciones: activar, suspender, cancelar tenant
- [ ] Vista de ingresos por tenant y plan

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-010 · Gestión de planes de suscripción
**Como** super admin, **quiero** definir planes de suscripción con límites y precios, **para** monetizar la plataforma.

**Criterios de aceptación:**
- [ ] Planes: Starter, Professional, Enterprise (configurable)
- [ ] Límites por plan: usuarios, embarques/mes, storage, módulos disponibles
- [ ] Precio mensual y anual con descuento configurable
- [ ] Cambio de plan aplicado al siguiente ciclo de facturación
- [ ] Alerta automática al acercarse al límite del plan

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-011 · Facturación recurrente de tenants (SaaS Billing)
**Como** super admin, **quiero** que el sistema genere facturas mensuales automáticas a cada tenant, **para** gestionar los ingresos de la plataforma.

**Criterios de aceptación:**
- [ ] Generación automática de factura al inicio de cada período
- [ ] Integración con pasarela de pago (Stripe / PayPal)
- [ ] Email de factura enviado automáticamente al admin del tenant
- [ ] Estado de pago: Pendiente, Pagado, Vencido, Fallido
- [ ] Suspensión automática tras 7 días de vencimiento sin pago
- [ ] Portal de autogestión de facturación para el tenant

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-012 · Onboarding wizard para nuevos tenants
**Como** nuevo administrador de tenant, **quiero** un asistente de configuración inicial, **para** configurar mi empresa rápidamente al activar la cuenta.

**Criterios de aceptación:**
- [ ] Paso 1: Datos de la empresa (nombre, país, moneda, zona horaria, idioma)
- [ ] Paso 2: Logo y personalización visual (colores primarios)
- [ ] Paso 3: Creación del primer usuario administrador
- [ ] Paso 4: Configuración de modos de transporte activos
- [ ] Paso 5: Invitación de usuarios iniciales
- [ ] Barra de progreso visible en todo el wizard
- [ ] El wizard puede retomarse si se interrumpe

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-013 · Gestión de usuarios por tenant
**Como** administrador de tenant, **quiero** crear, editar y desactivar usuarios de mi empresa, **para** gestionar el acceso al sistema.

**Criterios de aceptación:**
- [ ] CRUD completo de usuarios
- [ ] Asignación de rol, departamento y zona geográfica
- [ ] Invitación por email con link de activación (válido 48h)
- [ ] Desactivación sin eliminación (preserva historial)
- [ ] Límite de usuarios según el plan de suscripción
- [ ] Vista de último acceso por usuario

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-014 · Configuración general del tenant
**Como** administrador, **quiero** configurar los parámetros generales de mi empresa, **para** adaptar el sistema a mi operación.

**Criterios de aceptación:**
- [ ] Datos generales: nombre, RUC/NIT, dirección fiscal, teléfono
- [ ] Moneda principal y secundaria
- [ ] Zona horaria y formato de fecha/hora
- [ ] Idioma del sistema (ES / EN / PT)
- [ ] Logo de la empresa (PNG/SVG, máximo 2MB)
- [ ] Numeración de documentos (prefijos y consecutivos configurables)
- [ ] Email de notificaciones salientes (SMTP o SendGrid)

**Estimación:** 5 pts | **Prioridad:** Alta

---

---

# 📚 EP-03 — Gestión de Maestros (Catálogos Base)

## Sprint 3 — Catálogos del Sistema

---

### HU-015 · Gestión de ubicaciones y geocodificación
**Como** operador, **quiero** registrar ubicaciones (clientes, almacenes, puertos, aeropuertos), **para** usarlas como origen/destino en embarques.

**Criterios de aceptación:**
- [ ] Registro de: nombre, tipo, dirección, país, ciudad, código postal
- [ ] Geocodificación automática de dirección a coordenadas (lat/lng)
- [ ] Visualización en mapa interactivo
- [ ] Tipos: Almacén, Cliente, Puerto, Aeropuerto, Punto de Cruce, Otro
- [ ] Importación masiva desde CSV
- [ ] Búsqueda por nombre, código o coordenadas

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-016 · Gestión de zonas de entrega
**Como** planificador, **quiero** definir zonas geográficas de cobertura, **para** asignar carriers y tarifas por zona.

**Criterios de aceptación:**
- [ ] Creación de zonas por polígono en mapa o por lista de códigos postales
- [ ] Asignación de nombre, código y color identificador
- [ ] Una ubicación puede pertenecer a múltiples zonas
- [ ] Zonas usadas en reglas de tarifas y asignación de carriers
- [ ] Exportar/importar zonas en formato GeoJSON

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-017 · Catálogo de tipos de mercancía y productos
**Como** operador, **quiero** registrar tipos de mercancía con sus atributos, **para** clasificar correctamente los embarques.

**Criterios de aceptación:**
- [ ] Atributos: nombre, código, clase de peligrosidad, temperatura requerida, peso volumétrico
- [ ] Clasificación según código UN (mercancías peligrosas)
- [ ] Marcación: frágil, perecedero, requiere refrigeración, sobredimensionado
- [ ] Clasificación arancelaria (HS Code) opcional
- [ ] Importación masiva desde CSV
- [ ] Usado como referencia en órdenes de transporte

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-018 · Catálogo de unidades de medida y embalajes
**Como** operador, **quiero** gestionar unidades de medida y tipos de embalaje, **para** estandarizar el registro de cargas.

**Criterios de aceptación:**
- [ ] Unidades de peso: kg, lb, ton, g
- [ ] Unidades de volumen: m³, ft³, L
- [ ] Unidades de longitud: cm, in, m, ft
- [ ] Tipos de embalaje: Pallet, Caja, Tambor, Contenedor, A Granel, Bobina, Otro
- [ ] Factor de conversión entre unidades

**Estimación:** 3 pts | **Prioridad:** Alta

---

### HU-019 · Catálogo de clientes (Shippers)
**Como** ejecutivo de cuenta, **quiero** registrar clientes con toda su información comercial, **para** asociarlos a órdenes y contratos de transporte.

**Criterios de aceptación:**
- [ ] CRUD completo de clientes
- [ ] Datos: nombre, RUC/NIT, contactos, dirección fiscal, industria
- [ ] Clasificación: Regular, VIP, Ocasional
- [ ] Múltiples contactos por cliente con rol (Logística, Compras, Finanzas)
- [ ] Documentos adjuntos (contrato, RUC, etc.)
- [ ] Estado de crédito: Al día, En mora, Bloqueado
- [ ] Historial de embarques del cliente

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-020 · Catálogo de tarifas base
**Como** gerente de operaciones, **quiero** definir tarifas de transporte por zona, modo y tipo de servicio, **para** calcular costos automáticamente al planificar embarques.

**Criterios de aceptación:**
- [ ] Tarifa por: zona origen–destino, modo (FTL/LTL), tipo de servicio, peso/volumen
- [ ] Vigencia de tarifas: fecha inicio y fin
- [ ] Recargos configurables: combustible, peaje, manipulación, seguro, urgencia
- [ ] Moneda de tarifa con conversión automática
- [ ] Historial de cambios de tarifas
- [ ] Simulador de costos desde la misma pantalla

**Estimación:** 13 pts | **Prioridad:** Alta

---

---

# 📦 EP-04 — Order Management (Gestión de Órdenes)

## Sprint 4 — Creación y Gestión de Órdenes

---

### HU-021 · Creación manual de orden de transporte
**Como** operador de logística, **quiero** crear órdenes de transporte manualmente, **para** registrar solicitudes de envío de mis clientes.

**Criterios de aceptación:**
- [ ] Campos obligatorios: cliente, origen, destino, tipo de mercancía, cantidad, peso, volumen
- [ ] Campos opcionales: instrucciones especiales, valor declarado, referencia del cliente
- [ ] Selección de modo de transporte: Terrestre, Aéreo, Marítimo, Ferroviario, Intermodal
- [ ] Selección de nivel de servicio: Estándar, Express, Programado
- [ ] Fecha solicitada de pickup y fecha requerida de entrega
- [ ] Número de orden generado automáticamente con prefijo configurable
- [ ] Estado inicial: `DRAFT`

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-022 · Importación de órdenes desde CSV/Excel
**Como** operador, **quiero** importar órdenes masivamente desde un archivo CSV o Excel, **para** procesar grandes volúmenes sin captura manual.

**Criterios de aceptación:**
- [ ] Plantilla de importación descargable
- [ ] Validación de campos obligatorios antes de importar
- [ ] Reporte de errores por fila con descripción del problema
- [ ] Importación parcial: las filas válidas se crean, las inválidas se reportan
- [ ] Histórico de importaciones con resultado y usuario

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-023 · Recepción de órdenes por API (EDI/REST)
**Como** cliente enterprise, **quiero** enviar órdenes directamente desde mi ERP o sistema vía API, **para** automatizar la creación sin intervención manual.

**Criterios de aceptación:**
- [ ] Endpoint REST POST `/api/v1/orders` documentado en Swagger
- [ ] Autenticación por API Key del tenant
- [ ] Validación de payload y respuesta con errores detallados
- [ ] Soporte EDI 204 (Motor Carrier Load Tender)
- [ ] Webhook de confirmación de recepción hacia el sistema del cliente
- [ ] Rate limiting configurable por tenant

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-024 · Flujo de estados de la orden
**Como** operador, **quiero** que la orden avance por estados controlados, **para** tener trazabilidad completa del ciclo de vida del embarque.

**Criterios de aceptación:**
- [ ] Estados: `DRAFT → CONFIRMED → ASSIGNED → PICKUP_SCHEDULED → IN_TRANSIT → DELIVERED → INVOICED → CLOSED`
- [ ] Estados de excepción: `CANCELLED`, `ON_HOLD`, `FAILED_DELIVERY`
- [ ] Cada cambio de estado registra usuario, timestamp y motivo opcional
- [ ] Solo roles autorizados pueden mover a ciertos estados
- [ ] Notificación automática al cliente en cambios clave de estado

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-025 · Consolidación de órdenes
**Como** planificador, **quiero** consolidar múltiples órdenes en un solo embarque, **para** optimizar el uso de capacidad y reducir costos.

**Criterios de aceptación:**
- [ ] Selección múltiple de órdenes compatibles (misma zona, modo, fecha)
- [ ] Verificación automática de compatibilidad de carga (tipo, restricciones)
- [ ] Creación de un Shipment que agrupa las órdenes seleccionadas
- [ ] Las órdenes consolidadas quedan vinculadas al shipment padre
- [ ] Posibilidad de desconsolidar antes de asignar carrier

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-026 · División de órdenes (Split)
**Como** planificador, **quiero** dividir una orden en múltiples embarques parciales, **para** gestionar entregas parciales o por diferentes rutas.

**Criterios de aceptación:**
- [ ] Selección de la orden y definición de cantidades/pesos por cada split
- [ ] La suma de splits debe igualar la orden original
- [ ] Cada split genera un shipment independiente con referencia a la orden origen
- [ ] La orden original queda en estado `PARTIALLY_SPLIT`
- [ ] Historial visible de todos los splits desde la orden original

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-027 · Órdenes recurrentes y plantillas
**Como** operador, **quiero** guardar plantillas de órdenes frecuentes, **para** crear nuevas órdenes rápidamente sin reintroducir todos los datos.

**Criterios de aceptación:**
- [ ] Guardar cualquier orden como plantilla con nombre
- [ ] Crear nueva orden desde plantilla (todos los campos precargados)
- [ ] Configurar recurrencia: diaria, semanal, quincenal, mensual
- [ ] Las órdenes recurrentes se generan automáticamente en la fecha programada
- [ ] Notificación al operador cuando se crea una orden recurrente

**Estimación:** 5 pts | **Prioridad:** Media

---

## Sprint 5 — Órdenes Avanzadas

---

### HU-028 · Gestión de órdenes de compra vinculadas (PO Integration)
**Como** operador, **quiero** vincular órdenes de transporte a órdenes de compra o venta del cliente, **para** trazabilidad end-to-end.

**Criterios de aceptación:**
- [ ] Campo de referencia PO/SO en la orden de transporte
- [ ] Búsqueda de órdenes por número de PO
- [ ] Una PO puede vincularse a múltiples órdenes de transporte
- [ ] Vista de trazabilidad PO → Órdenes → Shipments → Entregas

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-029 · Priorización de órdenes
**Como** dispatcher, **quiero** asignar prioridades a las órdenes, **para** planificar primero los envíos más urgentes o importantes.

**Criterios de aceptación:**
- [ ] Niveles de prioridad: Crítico, Alto, Normal, Bajo
- [ ] Prioridad automática basada en: cliente VIP, fecha de entrega, valor de la carga
- [ ] Vista de órdenes ordenada por prioridad
- [ ] Alertas cuando órdenes de alta prioridad llevan más de X horas sin asignar

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-030 · Gestión de rechazos y re-entregas
**Como** operador, **quiero** registrar rechazos de entrega y gestionar re-entregas, **para** resolver incidencias de entrega fallida.

**Criterios de aceptación:**
- [ ] Registro de rechazo con motivo: cliente ausente, dirección incorrecta, mercancía dañada, rechazo del cliente
- [ ] El rechazo cambia el estado del shipment a `FAILED_DELIVERY`
- [ ] Creación de orden de re-entrega vinculada al shipment original
- [ ] Notificación automática al cliente y al planificador
- [ ] Cargo adicional por re-entrega calculado automáticamente

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-031 · SLA Management por cliente
**Como** gerente de operaciones, **quiero** definir SLAs de entrega por cliente, **para** monitorear el cumplimiento de compromisos contractuales.

**Criterios de aceptación:**
- [ ] Definición de SLA por cliente: tiempo máximo de entrega, ventana horaria, penalidades
- [ ] Alerta automática cuando una orden está en riesgo de incumplir su SLA
- [ ] KPI de cumplimiento de SLA por cliente en dashboard
- [ ] Reporte mensual de SLA por cliente exportable

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-032 · Gestión de reclamos (Claims Management)
**Como** cliente, **quiero** registrar reclamos por daño, pérdida o retraso, **para** obtener compensación o solución formal.

**Criterios de aceptación:**
- [ ] Formulario de reclamo: tipo (daño/pérdida/retraso), descripción, evidencia fotográfica, monto reclamado
- [ ] Flujo de aprobación: Abierto → En revisión → Aprobado/Rechazado → Cerrado
- [ ] Notificaciones al cliente en cada cambio de estado
- [ ] Vinculación al shipment afectado
- [ ] Reporte de reclamos por período, tipo y resolución

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 🚛 EP-05 — Carrier Management (Gestión de Transportistas)

## Sprint 6 — Gestión de Carriers

---

### HU-033 · Registro de transportistas y flota propia
**Como** administrador, **quiero** registrar los transportistas disponibles (propios y terceros), **para** asignarlos a embarques según disponibilidad y capacidad.

**Criterios de aceptación:**
- [ ] Datos del transportista: nombre, tipo (propio/tercero), RUC, contacto, dirección
- [ ] Clasificación: FTL, LTL, Aéreo, Marítimo, Especializado
- [ ] Zonas de cobertura geográfica del carrier
- [ ] Modos de transporte que opera
- [ ] Estado: Activo, Inactivo, Bloqueado, En evaluación

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-034 · Gestión documental de transportistas
**Como** administrador, **quiero** adjuntar y controlar documentos vigentes de cada transportista, **para** garantizar que operan con cumplimiento legal.

**Criterios de aceptación:**
- [ ] Documentos: licencia de operación, póliza de seguro, habilitación vehicular, certificados de calidad
- [ ] Fecha de vencimiento por documento
- [ ] Alertas automáticas: 30, 15 y 7 días antes del vencimiento
- [ ] Documento vencido bloquea asignación del carrier automáticamente
- [ ] Historial de documentos cargados con usuario y fecha

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-035 · Calificación y scorecard de carriers
**Como** gerente de operaciones, **quiero** evaluar el desempeño de cada carrier automáticamente, **para** tomar decisiones de asignación basadas en rendimiento.

**Criterios de aceptación:**
- [ ] KPIs evaluados: OTD (On-Time Delivery), daños reportados, documentos completos, respuesta a asignación
- [ ] Score automático de 0–100 calculado por período
- [ ] Clasificación: Oro, Plata, Bronce, En observación
- [ ] Historial de puntajes por carrier
- [ ] Reporte comparativo de carriers exportable

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-036 · Contratos y tarifas por carrier
**Como** gerente de compras, **quiero** registrar contratos y tarifas negociadas con cada carrier, **para** calcular automáticamente el costo de flete al asignar.

**Criterios de aceptación:**
- [ ] Contrato con vigencia, condiciones de pago, penalidades
- [ ] Tarifas por: ruta, zona, modo, peso, distancia
- [ ] Tarifas base + recargos (combustible, peaje, seguro)
- [ ] Múltiples vigencias de tarifa (la tarifa activa se aplica automáticamente)
- [ ] Alerta de vencimiento de contrato 60 días antes

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-037 · Licitación de cargas (Spot Bidding / RFQ)
**Como** dispatcher, **quiero** publicar una carga disponible y recibir ofertas de múltiples carriers, **para** seleccionar la mejor opción en precio y tiempo.

**Criterios de aceptación:**
- [ ] Publicación de RFQ con detalles de la carga y fecha límite de oferta
- [ ] Notificación automática a carriers habilitados de la zona
- [ ] Portal del carrier para ver y ofertar en RFQs activos
- [ ] Comparador de ofertas recibidas (precio, tiempo, rating del carrier)
- [ ] Aceptación de oferta y asignación automática del carrier
- [ ] Notificación de rechazo a carriers no seleccionados

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-038 · Portal del transportista
**Como** carrier externo, **quiero** acceder a un portal web para gestionar mis asignaciones, **para** operar sin necesidad de comunicación manual.

**Criterios de aceptación:**
- [ ] Acceso con credenciales propias (email + contraseña)
- [ ] Vista de cargas asignadas y disponibles para ofertar
- [ ] Aceptar o rechazar asignaciones con motivo
- [ ] Carga de documentos del viaje (carta de porte, manifiesto)
- [ ] Carga de POD (firma + foto) al completar entrega
- [ ] Historial de viajes y facturas emitidas

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-039 · Blacklist y control de carriers
**Como** administrador, **quiero** bloquear carriers específicos, **para** evitar su asignación por razones de incumplimiento o riesgo.

**Criterios de aceptación:**
- [ ] Bloqueo de carrier con motivo obligatorio y usuario que aplica el bloqueo
- [ ] Carrier bloqueado no aparece en opciones de asignación
- [ ] Historial de bloqueos con fechas y motivos
- [ ] Desbloqueo con motivo y aprobación de nivel superior
- [ ] Notificación al carrier sobre su estado

**Estimación:** 3 pts | **Prioridad:** Media

---

---

# 🗓️ EP-06 — Shipment Planning (Planificación de Embarques)

## Sprint 7 — Planificación Core

---

### HU-040 · Creación de embarque (Shipment)
**Como** dispatcher, **quiero** crear un embarque a partir de una o más órdenes confirmadas, **para** organizar la operación de transporte.

**Criterios de aceptación:**
- [ ] Un embarque puede contener una o múltiples órdenes
- [ ] Selección de modo de transporte, tipo de vehículo requerido
- [ ] Fechas de pickup y entrega planificadas
- [ ] Cálculo automático de peso y volumen total
- [ ] Verificación de capacidad del vehículo seleccionado
- [ ] Número de embarque generado automáticamente
- [ ] Estado inicial: `PLANNED`

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-041 · Load Planning — optimización de carga
**Como** planificador, **quiero** optimizar la distribución de carga en el vehículo, **para** maximizar el uso de capacidad y garantizar seguridad de la mercancía.

**Criterios de aceptación:**
- [ ] Visualización 2D/3D del espacio de carga del vehículo
- [ ] Algoritmo de bin-packing para sugerir distribución óptima
- [ ] Restricciones: peso máximo por eje, mercancía incompatible, frágiles arriba
- [ ] Indicador de capacidad usada (%) en peso y volumen
- [ ] Advertencia si se supera la capacidad del vehículo

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-042 · Planificación multi-stop
**Como** dispatcher, **quiero** planificar rutas con múltiples paradas de carga y descarga, **para** optimizar un solo viaje con múltiples clientes.

**Criterios de aceptación:**
- [ ] Agregar múltiples paradas de tipo: Pickup, Delivery, Parada técnica
- [ ] Orden de paradas editable (drag & drop)
- [ ] Cálculo de tiempo total de ruta con ventanas de tiempo por parada
- [ ] Verificación de que la carga de cada parada no exceda la capacidad disponible
- [ ] Mapa con visualización de todas las paradas en orden

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-043 · Gestión de ventanas de tiempo (Time Windows)
**Como** planificador, **quiero** definir ventanas horarias para cada parada, **para** que las entregas respeten los horarios del cliente.

**Criterios de aceptación:**
- [ ] Ventana de tiempo por parada: hora más temprana y hora más tardía
- [ ] El sistema alerta si la ruta planificada no puede cumplir alguna ventana
- [ ] Restricciones horarias por ubicación (ej: almacén solo recibe 8am–5pm)
- [ ] Consideración de tiempo de servicio (carga/descarga) por parada

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-044 · Asignación de carrier y vehículo al embarque
**Como** dispatcher, **quiero** asignar un carrier, vehículo y conductor a cada embarque, **para** formalizar la operación y notificar al transportista.

**Criterios de aceptación:**
- [ ] Búsqueda de carriers disponibles (sin conflicto de agenda) para la ruta
- [ ] Filtros: zona de cobertura, tipo de vehículo, rating, tarifa
- [ ] Asignación manual o mediante sugerencia automática del sistema
- [ ] Notificación al carrier/conductor de la asignación
- [ ] Generación automática de la orden de transporte para el carrier
- [ ] El carrier puede aceptar o rechazar desde su portal

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-045 · Backhaul Planning (planificación de retorno)
**Como** planificador, **quiero** identificar oportunidades de carga para el retorno del vehículo, **para** reducir viajes vacíos y costos.

**Criterios de aceptación:**
- [ ] Vista de vehículos con retorno vacío en los próximos N días
- [ ] Órdenes disponibles compatibles con la ruta de retorno (origen cercano al destino del viaje)
- [ ] Sugerencia automática de cargas de backhaul
- [ ] Indicador de ahorro estimado al aprovechar el retorno
- [ ] Asignación de carga de backhaul al mismo embarque o uno nuevo

**Estimación:** 8 pts | **Prioridad:** Media

---

## Sprint 8 — Planificación Avanzada

---

### HU-046 · Intermodalidad (planificación multi-modo)
**Como** planificador, **quiero** combinar múltiples modos de transporte en un solo embarque, **para** optimizar rutas de larga distancia o internacionales.

**Criterios de aceptación:**
- [ ] Creación de embarque con tramos: Terrestre + Marítimo, Terrestre + Aéreo, etc.
- [ ] Cada tramo tiene su propio carrier, vehículo y fechas
- [ ] Punto de transbordo (puerto, aeropuerto, terminal) registrado con tiempo de estadía
- [ ] Cálculo de costo total sumando todos los tramos
- [ ] Trazabilidad de la carga a través de todos los tramos

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-047 · Gestión de contenedores
**Como** agente de carga, **quiero** gestionar contenedores marítimos, **para** controlar el ciclo de vida de cada unidad de carga.

**Criterios de aceptación:**
- [ ] Registro de contenedor: número, tipo (20', 40', 40'HC, Reefer), propietario, estado
- [ ] Asignación de contenedor a embarque marítimo
- [ ] Seguimiento: en terminal, en tránsito, en descarga, devuelto
- [ ] Alertas de devolución por fecha límite (demurrage)
- [ ] Costos de demurrage y detention calculados automáticamente

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-048 · Programación de citas de carga (Appointments)
**Como** planificador, **quiero** programar citas para carga y descarga en almacenes y muelles, **para** evitar congestión y tiempos de espera.

**Criterios de aceptación:**
- [ ] Calendario de disponibilidad de muelles por almacén
- [ ] Programación de cita: muelle, fecha, hora, duración estimada, tipo (carga/descarga)
- [ ] Confirmación automática al carrier con detalles de la cita
- [ ] El carrier puede ver sus citas en el portal
- [ ] Alerta si el vehículo llega fuera de la ventana programada

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 🗺️ EP-07 — Route Optimization (Optimización de Rutas)

## Sprint 9 — Motor de Ruteo

---

### HU-049 · Motor de optimización de rutas (VRP)
**Como** planificador, **quiero** que el sistema calcule la ruta óptima para múltiples embarques, **para** minimizar distancia, tiempo y costo de combustible.

**Criterios de aceptación:**
- [ ] Algoritmo VRP (Vehicle Routing Problem) con restricciones reales
- [ ] Restricciones consideradas: capacidad, ventanas de tiempo, tipo de vehículo
- [ ] Optimización por: menor distancia, menor tiempo, menor costo
- [ ] Resultado en segundos para flotas de hasta 50 vehículos y 200 paradas
- [ ] Comparación de escenarios: ruta actual vs. ruta optimizada (ahorro estimado)

**Estimación:** 21 pts | **Prioridad:** Alta

---

### HU-050 · Mapas interactivos y visualización de rutas
**Como** dispatcher, **quiero** visualizar todas las rutas activas en un mapa en tiempo real, **para** tener visibilidad completa de la operación.

**Criterios de aceptación:**
- [ ] Mapa con posición de todos los vehículos en tiempo real
- [ ] Ruta planificada vs. ruta real (trayecto recorrido)
- [ ] Colores diferenciados por estado del embarque
- [ ] Click en vehículo muestra: conductor, carga, ETA, próxima parada
- [ ] Filtros: por carrier, por zona, por estado del embarque
- [ ] Capas de mapa: tráfico en tiempo real, zonas restringidas

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-051 · Geocodificación y validación de direcciones
**Como** operador, **quiero** que el sistema valide y geocodifique direcciones automáticamente, **para** evitar errores de entrega por direcciones incorrectas.

**Criterios de aceptación:**
- [ ] Geocodificación automática al ingresar dirección (integración con Google Maps / OpenStreetMap)
- [ ] Sugerencias de dirección al escribir (autocomplete)
- [ ] Alerta si la dirección no puede ser geocodificada
- [ ] Corrección manual con ajuste de pin en mapa
- [ ] Validación de cobertura en la zona del carrier asignado

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-052 · Cálculo de ETA dinámico
**Como** cliente, **quiero** conocer la hora estimada de llegada actualizada de mi embarque, **para** coordinar la recepción.

**Criterios de aceptación:**
- [ ] ETA calculado en tiempo real considerando posición del vehículo + tráfico
- [ ] Recálculo automático cada 5 minutos
- [ ] Notificación al cliente cuando el ETA varía más de 30 minutos
- [ ] ETA visible en el portal del cliente y en la app del conductor
- [ ] Historial de ETAs para análisis de exactitud

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-053 · Ruteo dinámico y re-planificación en tiempo real
**Como** dispatcher, **quiero** re-planificar rutas en tiempo real ante incidentes, **para** minimizar el impacto en las entregas.

**Criterios de aceptación:**
- [ ] Detección automática de incidentes: tráfico severo, accidente, cierre de vía
- [ ] Sugerencia de ruta alternativa con ETA comparativo
- [ ] Dispatcher aprueba o rechaza la ruta alternativa
- [ ] Actualización inmediata en el GPS del conductor
- [ ] Registro del incidente y la solución aplicada

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-054 · Zonas de restricción y rutas prohibidas
**Como** planificador, **quiero** definir zonas y rutas prohibidas, **para** que el motor de optimización las evite en el ruteo.

**Criterios de aceptación:**
- [ ] Definición de zonas de restricción en mapa (polígono)
- [ ] Restricciones por tipo de vehículo (peso, altura, mercancía peligrosa)
- [ ] Restricciones por horario (zonas de carga/descarga urbana)
- [ ] El motor de ruteo respeta las restricciones al calcular rutas
- [ ] Alerta si el conductor se acerca a una zona restringida

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 📡 EP-08 — Track & Trace (Rastreo en Tiempo Real)

## Sprint 10 — Visibilidad Total

---

### HU-055 · Rastreo GPS en tiempo real de vehículos
**Como** dispatcher, **quiero** ver la posición exacta de todos los vehículos en tiempo real, **para** monitorear la operación desde el centro de control.

**Criterios de aceptación:**
- [ ] Integración con dispositivos GPS (Samsara, Geotab, Trimble, Calamp)
- [ ] Actualización de posición cada 30 segundos (configurable)
- [ ] Mapa en tiempo real con icono de vehículo y dirección de movimiento
- [ ] Velocidad actual, dirección y estado del motor
- [ ] Historial de recorrido del día (breadcrumb trail)

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-056 · Eventos de trayecto y milestone tracking
**Como** operador, **quiero** registrar eventos clave durante el trayecto, **para** tener trazabilidad completa del progreso del embarque.

**Criterios de aceptación:**
- [ ] Eventos automáticos por GPS: salida de origen, llegada a parada, salida de parada, llegada a destino
- [ ] Eventos manuales: carga completada, problema en ruta, parada técnica, avería
- [ ] Cada evento registra: tipo, timestamp, posición GPS, usuario/sistema que lo generó
- [ ] Timeline de eventos visible en el detalle del embarque
- [ ] Notificaciones automáticas al cliente en eventos clave

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-057 · Geofencing y alertas de zona
**Como** dispatcher, **quiero** configurar geofences alrededor de ubicaciones clave, **para** recibir alertas automáticas cuando un vehículo entra o sale.

**Criterios de aceptación:**
- [ ] Creación de geofence circular o poligonal desde el mapa
- [ ] Alertas configurables: entrada, salida, tiempo de permanencia excedido
- [ ] Canales de alerta: notificación en app, email, SMS
- [ ] Registro de todos los eventos de geofence con timestamp
- [ ] Geofences reutilizables para múltiples embarques

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-058 · Portal de rastreo para clientes (Customer Tracking)
**Como** cliente, **quiero** rastrear mis embarques en tiempo real sin necesidad de contactar al operador, **para** coordinar la recepción con autonomía.

**Criterios de aceptación:**
- [ ] Acceso por link único (no requiere login) o desde el portal del cliente
- [ ] Vista de mapa con posición del vehículo en tiempo real
- [ ] ETA actualizado dinámicamente
- [ ] Historial de eventos del embarque
- [ ] Personalizado con logo y colores del tenant
- [ ] Soporte móvil (responsive)

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-059 · Alertas de comportamiento del conductor
**Como** gerente de flota, **quiero** recibir alertas sobre comportamiento de riesgo del conductor, **para** gestionar seguridad vial.

**Criterios de aceptación:**
- [ ] Alertas detectadas: exceso de velocidad, frenado brusco, aceleración brusca, parada no programada
- [ ] Umbral configurable por tipo de alerta
- [ ] Notificación en tiempo real al dispatcher
- [ ] Score de conducción por conductor calculado automáticamente
- [ ] Reporte mensual de comportamiento por conductor

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 📄 EP-09 — Document Management (Gestión Documental)

## Sprint 11 — Documentación Operacional

---

### HU-060 · Generación de Carta de Porte / Bill of Lading
**Como** operador, **quiero** generar la Carta de Porte o Bill of Lading automáticamente al confirmar un embarque, **para** cumplir con el requisito legal de transporte.

**Criterios de aceptación:**
- [ ] Generación en PDF con todos los datos del embarque
- [ ] Personalizable con logo y datos del tenant
- [ ] Numeración automática correlativa
- [ ] Campos según normativa local (configurable por país)
- [ ] Firma digital embebida del emisor
- [ ] Envío automático por email al carrier y al cliente

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-061 · Generación de Manifiesto de Carga
**Como** operador, **quiero** generar el manifiesto de carga por vehículo o embarque, **para** tener el resumen oficial de toda la mercancía transportada.

**Criterios de aceptación:**
- [ ] Manifiesto incluye: todas las órdenes, descripción de mercancía, peso, volumen, destinatarios
- [ ] Generación en PDF con número único
- [ ] Firma del conductor al recibir la carga
- [ ] Versión digital enviada al carrier y al conductor
- [ ] Adjunto automáticamente al registro del embarque

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-062 · Proof of Delivery (POD) digital
**Como** conductor, **quiero** capturar la prueba de entrega digital desde mi app, **para** confirmar la entrega sin papel físico.

**Criterios de aceptación:**
- [ ] Captura de firma digital del receptor en la app
- [ ] Foto de la mercancía entregada
- [ ] Datos del receptor: nombre, cédula/ID, cargo
- [ ] Fecha y hora automáticas con timestamp del servidor
- [ ] Posición GPS en el momento de la entrega
- [ ] POD sincronizado automáticamente al sistema central
- [ ] PDF del POD generado y enviado al cliente automáticamente

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-063 · Repositorio documental centralizado
**Como** operador, **quiero** un repositorio de todos los documentos generados y cargados por embarque, **para** acceder a cualquier documento de forma inmediata.

**Criterios de aceptación:**
- [ ] Cada embarque tiene su carpeta documental: carta de porte, manifiesto, POD, fotos, facturas
- [ ] Carga manual de documentos adicionales con tipo y descripción
- [ ] Versionado de documentos (se puede subir una nueva versión sin eliminar la anterior)
- [ ] Búsqueda de documentos por número de embarque, tipo o fecha
- [ ] Descarga individual o en ZIP de todos los documentos de un embarque
- [ ] Retención configurable: mínimo 5 años

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-064 · Generación de Packing List
**Como** operador, **quiero** generar el packing list de cada embarque, **para** detallar el contenido exacto de la carga.

**Criterios de aceptación:**
- [ ] Detalle por ítem: descripción, cantidad, peso unitario, peso total, dimensiones, número de bultos
- [ ] Agrupación por destinatario en embarques multi-stop
- [ ] Generación en PDF con logo del tenant
- [ ] Código QR o barcode por línea de ítem (opcional)

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-065 · Firma electrónica de documentos
**Como** administrador, **quiero** integrar firma electrónica en documentos clave, **para** dar validez legal a los documentos sin papel.

**Criterios de aceptación:**
- [ ] Firma electrónica simple en app de conductor (POD)
- [ ] Firma electrónica avanzada para contratos con carriers (integración DocuSign / Adobe Sign)
- [ ] Registro de: firmante, timestamp, IP, hash del documento
- [ ] Documento firmado bloqueado para edición
- [ ] Descarga del documento con certificado de firma embebido

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 💰 EP-10 — Freight Audit & Payment

## Sprint 12 — Auditoría de Flete

---

### HU-066 · Cálculo automático de costo de flete
**Como** planificador, **quiero** que el sistema calcule el costo de flete automáticamente al asignar carrier, **para** conocer el costo real antes de confirmar la operación.

**Criterios de aceptación:**
- [ ] Cálculo basado en: tarifa del contrato del carrier, distancia, peso/volumen, tipo de servicio
- [ ] Aplicación automática de recargos vigentes: combustible (fuel surcharge), peaje, seguro, manipulación
- [ ] Conversión de moneda si la tarifa está en diferente moneda
- [ ] Desglose detallado de costo en pantalla
- [ ] Comparativo de costo entre carriers disponibles

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-067 · Auditoría de facturas de flete (Freight Audit)
**Como** auditor, **quiero** que el sistema compare automáticamente la factura del carrier contra el contrato y lo ejecutado, **para** detectar cobros incorrectos.

**Criterios de aceptación:**
- [ ] Carga de factura del carrier (PDF o EDI 210)
- [ ] Comparación automática: tarifa facturada vs. tarifa contratada vs. costo calculado
- [ ] Detección de discrepancias: sobrecobro, tarifa incorrecta, recargo no pactado
- [ ] Estados de auditoría: Pendiente, Aprobada, Disputada, Ajustada
- [ ] Reporte de discrepancias con monto total en disputa

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-068 · Workflow de aprobación de pagos a carriers
**Como** gerente financiero, **quiero** que los pagos a carriers pasen por un flujo de aprobación, **para** garantizar control financiero.

**Criterios de aceptación:**
- [ ] Flujo configurable: Operaciones aprueba → Finanzas valida → Gerencia autoriza (según monto)
- [ ] Notificación por email a cada aprobador en su turno
- [ ] Rechazo con motivo obligatorio y notificación al paso anterior
- [ ] Límites de aprobación por rol (ej: Operaciones hasta $5,000, Finanzas hasta $50,000)
- [ ] Registro completo del flujo con timestamps y usuarios

**Estimación:** 8 pts | **Prioridad:** Media

---

## Sprint 13 — Facturación a Clientes

---

### HU-069 · Generación de facturas a clientes
**Como** facturador, **quiero** generar facturas a mis clientes automáticamente al cerrar un embarque, **para** agilizar el proceso de cobro.

**Criterios de aceptación:**
- [ ] Factura generada en PDF con datos fiscales del tenant y del cliente
- [ ] Detalle: servicios prestados, tarifas, recargos, impuestos (IVA configurable)
- [ ] Numeración correlativa configurable por prefijo
- [ ] Múltiples embarques en una sola factura (facturación consolidada)
- [ ] Envío automático por email al contacto de facturación del cliente
- [ ] Integración con EDI 210 (Carrier Invoice)

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-070 · Gestión de cuentas por cobrar
**Como** gerente financiero, **quiero** ver el estado de todas las facturas emitidas, **para** gestionar la cartera de cobros.

**Criterios de aceptación:**
- [ ] Lista de facturas con estado: Emitida, Enviada, Vencida, Pagada, Anulada
- [ ] Días de vencimiento y monto pendiente por cliente
- [ ] Recordatorios automáticos de cobro a X, Y, Z días del vencimiento (configurable)
- [ ] Registro de pagos recibidos parciales o totales
- [ ] Dashboard de cartera: total por cobrar, vencido, por vencer

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-071 · Conciliación financiera operativa
**Como** controller financiero, **quiero** conciliar automáticamente lo planificado vs. ejecutado vs. facturado, **para** identificar desviaciones de costo.

**Criterios de aceptación:**
- [ ] Reporte de conciliación: costo estimado vs. costo real vs. facturado al cliente
- [ ] Margen por embarque, por cliente, por carrier
- [ ] Identificación de embarques no facturados después de X días de entregados
- [ ] Exportación a Excel para revisión financiera detallada

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 🌐 EP-11 — Customer Portal & CRM

## Sprint 14 — Portal del Cliente

---

### HU-072 · Portal self-service del cliente
**Como** cliente, **quiero** acceder a un portal web para gestionar mis envíos de forma autónoma, **para** reducir dependencia de llamadas y emails al operador.

**Criterios de aceptación:**
- [ ] Login con credenciales propias del cliente
- [ ] Dashboard con resumen: embarques activos, pendientes de pickup, entregados esta semana
- [ ] Creación de nueva solicitud de transporte desde el portal
- [ ] Rastreo en tiempo real de todos sus embarques activos
- [ ] Descarga de documentos: facturas, cartas de porte, PODs
- [ ] Historial de embarques con búsqueda y filtros
- [ ] Personalizado con logo y colores del tenant

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-073 · Cotización online para clientes
**Como** cliente, **quiero** solicitar una cotización de flete desde el portal, **para** conocer el costo antes de confirmar un envío.

**Criterios de aceptación:**
- [ ] Formulario: origen, destino, tipo de mercancía, peso, volumen, fecha de envío
- [ ] Cotización automática basada en tarifas publicadas del tenant
- [ ] Múltiples opciones de servicio: Estándar, Express, Programado
- [ ] Cotización válida por X horas (configurable)
- [ ] Conversión de cotización a orden con un clic
- [ ] Si no hay tarifa automática, la cotización va a revisión manual del operador

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-074 · Notificaciones automáticas al cliente
**Como** cliente, **quiero** recibir notificaciones automáticas sobre el estado de mis embarques, **para** estar informado sin consultar activamente.

**Criterios de aceptación:**
- [ ] Canales: email, SMS, WhatsApp (configurable por cliente)
- [ ] Eventos notificados: orden confirmada, pickup realizado, en tránsito, por llegar (1h antes), entregado, excepción
- [ ] Plantillas de notificación personalizables por el tenant
- [ ] El cliente puede configurar qué notificaciones recibir
- [ ] Historial de notificaciones enviadas por embarque

**Estimación:** 8 pts | **Prioridad:** Alta

---

---

# 🏭 EP-12 — Warehouse & Dock Management

## Sprint 15 — Gestión de Almacén y Muelles

---

### HU-075 · Gestión de muelles (Dock Management)
**Como** jefe de almacén, **quiero** gestionar la disponibilidad de muelles de carga y descarga, **para** evitar congestión y reducir tiempos de espera.

**Criterios de aceptación:**
- [ ] Registro de muelles por almacén: número, tipo (carga/descarga/ambos), capacidad
- [ ] Calendario visual de disponibilidad de muelles por hora
- [ ] Asignación de muelle a embarque programado
- [ ] Alertas de conflicto de muelle (doble asignación)
- [ ] Tiempo real de ocupación por muelle

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-076 · Yard Management (gestión de patios)
**Como** jefe de patio, **quiero** controlar la posición de vehículos y remolques en el patio, **para** optimizar el uso del espacio y reducir movimientos innecesarios.

**Criterios de aceptación:**
- [ ] Mapa digital del patio con posiciones disponibles
- [ ] Registro de entrada/salida de vehículos al patio (con hora y guardia)
- [ ] Asignación de posición en patio a cada vehículo/remolque
- [ ] Vista en tiempo real de ocupación del patio (%)
- [ ] Historial de movimientos de patio por vehículo

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-077 · Cross-docking
**Como** planificador, **quiero** gestionar operaciones de cross-docking, **para** transferir mercancía directamente entre vehículos sin almacenamiento intermedio.

**Criterios de aceptación:**
- [ ] Planificación de cross-dock: vehículo entrante, muelles, vehículo saliente, mercancía a transferir
- [ ] Ventana de tiempo de cross-dock (tiempo entre llegada entrada y salida)
- [ ] Verificación de compatibilidad de mercancía
- [ ] Registro de la operación con escaneo de bultos (código de barras/QR)
- [ ] Alertas si el vehículo de salida no está disponible a tiempo

**Estimación:** 8 pts | **Prioridad:** Baja

---

---

# 🌍 EP-13 — Comercio Internacional & Aduanas

## Sprint 16 — Operaciones Internacionales

---

### HU-078 · Gestión de importaciones y exportaciones
**Como** agente de comercio exterior, **quiero** gestionar el ciclo completo de importación y exportación, **para** controlar todas las etapas del proceso.

**Criterios de aceptación:**
- [ ] Flujo de exportación: Orden → Documentación → Pre-alerta → Embarque → Aduana origen → Tránsito → Aduana destino → Entrega
- [ ] Flujo de importación: Llegada → Pre-despacho → Revisión aduanal → Pago de aranceles → Retiro → Entrega
- [ ] Vinculación de documentos a cada etapa
- [ ] Trazabilidad completa del proceso

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-079 · Gestión de documentos de comercio exterior
**Como** agente aduanal, **quiero** gestionar todos los documentos requeridos para exportación/importación, **para** asegurar el cumplimiento regulatorio.

**Criterios de aceptación:**
- [ ] Documentos soportados: BL, AWB, Factura Comercial, Packing List, Certificado de Origen, Seguro, DUCA/DAI
- [ ] Check-list de documentos requeridos por tipo de operación y país
- [ ] Alerta de documentos faltantes antes del embarque
- [ ] Envío electrónico de documentos a autoridades aduaneras (donde aplique)

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-080 · Clasificación arancelaria (HS Code)
**Como** despachante de aduana, **quiero** gestionar la clasificación arancelaria de las mercancías, **para** determinar aranceles e impuestos correctamente.

**Criterios de aceptación:**
- [ ] Catálogo de HS Codes con descripción
- [ ] Asignación de HS Code a tipos de mercancía
- [ ] Cálculo estimado de aranceles e impuestos por HS Code y país de destino
- [ ] Historial de clasificaciones por mercancía

**Estimación:** 8 pts | **Prioridad:** Baja

---

### HU-081 · Gestión de brokers aduanales
**Como** gerente de operaciones internacionales, **quiero** gestionar mis agentes aduaneros, **para** asignarlos a operaciones y hacer seguimiento de su gestión.

**Criterios de aceptación:**
- [ ] Registro de brokers: nombre, país, licencia, contacto, tarifas
- [ ] Asignación de broker a operación de importación/exportación
- [ ] Portal del broker para subir documentos y actualizar estado
- [ ] Evaluación de desempeño del broker por operación

**Estimación:** 5 pts | **Prioridad:** Baja

---

### HU-082 · Gestión de Incoterms
**Como** comercial, **quiero** registrar el Incoterm de cada operación, **para** determinar responsabilidades y costos correctamente.

**Criterios de aceptación:**
- [ ] Catálogo de Incoterms vigentes (2020): EXW, FCA, FOB, CIF, DAP, DDP, etc.
- [ ] Selección de Incoterm en la orden de transporte internacional
- [ ] Descripción de responsabilidades por Incoterm visible en pantalla
- [ ] Impacto del Incoterm en el cálculo de costos y seguros

**Estimación:** 3 pts | **Prioridad:** Media

---

---

# 🚗 EP-14 — Fleet & Driver Management

## Sprint 17 — Gestión de Flota y Conductores

---

### HU-083 · Gestión de vehículos de flota propia
**Como** jefe de flota, **quiero** registrar y gestionar todos los vehículos de la empresa, **para** controlar disponibilidad y características.

**Criterios de aceptación:**
- [ ] Datos del vehículo: placa, tipo, marca, modelo, año, capacidad (peso/volumen), número de ejes
- [ ] Documentos: tarjeta de circulación, seguro, revisión técnica (con vencimientos)
- [ ] Estado: Disponible, En ruta, En mantenimiento, Fuera de servicio
- [ ] Asignación de vehículo a carrier o a flota propia
- [ ] Historial de embarques por vehículo

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-084 · Gestión de conductores
**Como** jefe de flota, **quiero** gestionar el perfil y documentos de cada conductor, **para** garantizar que están habilitados para operar.

**Criterios de aceptación:**
- [ ] Datos del conductor: nombre, DUI/cédula, licencia (tipo y vencimiento), teléfono, foto
- [ ] Documentos: licencia, antecedentes penales, examen médico, capacitaciones
- [ ] Alertas de vencimiento de documentos (30, 15, 7 días antes)
- [ ] Conductor con documentos vencidos no puede ser asignado
- [ ] Historial de viajes por conductor

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-085 · Control de disponibilidad de conductores y vehículos
**Como** dispatcher, **quiero** ver qué conductores y vehículos están disponibles para asignar, **para** planificar sin conflictos.

**Criterios de aceptación:**
- [ ] Vista de calendario de disponibilidad por conductor y vehículo
- [ ] Un conductor/vehículo no puede ser asignado a dos embarques simultáneos
- [ ] Bloqueo de disponibilidad por: vacaciones, mantenimiento, incapacidad
- [ ] Filtros: disponible hoy, disponible en rango de fechas, por tipo de vehículo

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-086 · Mantenimiento preventivo de vehículos
**Como** jefe de flota, **quiero** programar y registrar mantenimientos preventivos, **para** reducir averías en ruta y prolongar la vida útil de la flota.

**Criterios de aceptación:**
- [ ] Plan de mantenimiento por vehículo: tipo, intervalo (km o días), próximo servicio
- [ ] Alertas automáticas cuando se acerca el próximo mantenimiento
- [ ] Registro de mantenimientos realizados: fecha, taller, costo, descripción
- [ ] Vehículo en mantenimiento queda automáticamente no disponible
- [ ] Costo acumulado de mantenimiento por vehículo y período

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-087 · Control de combustible y rendimiento
**Como** gerente de flota, **quiero** controlar el consumo de combustible de cada vehículo, **para** detectar ineficiencias y reducir costos operativos.

**Criterios de aceptación:**
- [ ] Registro de cargas de combustible: fecha, vehículo, litros, costo, kilometraje
- [ ] Cálculo automático de rendimiento (km/litro) por carga
- [ ] Comparativo de rendimiento vs. rendimiento esperado del vehículo
- [ ] Alerta si el rendimiento cae más de X% del histórico (posible desvío)
- [ ] Costo de combustible por viaje y por km recorrido
- [ ] Dashboard de consumo de flota por período

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 🛡️ EP-15 — Compliance & Safety

## Sprint 18 — Seguridad y Cumplimiento

---

### HU-088 · Control de horas de conducción (HOS)
**Como** gerente de seguridad, **quiero** controlar las horas de servicio de los conductores, **para** cumplir regulaciones de seguridad vial y prevenir fatiga.

**Criterios de aceptación:**
- [ ] Registro de horas trabajadas por conductor: conducción, descanso, en servicio, fuera de servicio
- [ ] Límites configurables por país/regulación (ej: máx. 11h conducción / 14h en servicio)
- [ ] Alerta al conductor y dispatcher cuando se acerca el límite
- [ ] Bloqueo de asignación si el conductor no ha descansado el mínimo requerido
- [ ] Reporte de cumplimiento HOS por conductor y período

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-089 · Gestión de incidentes y accidentes
**Como** gerente de operaciones, **quiero** registrar y hacer seguimiento de incidentes y accidentes, **para** gestionarlos adecuadamente y aprender de ellos.

**Criterios de aceptación:**
- [ ] Registro del incidente: tipo, descripción, vehículo, conductor, ubicación GPS, fecha/hora
- [ ] Adjuntar fotos y documentos del incidente
- [ ] Tipos: accidente de tránsito, robo, avería, daño a mercancía, lesión, otro
- [ ] Flujo de seguimiento: Reportado → En investigación → Cerrado
- [ ] Notificación automática a gerencia en incidentes graves
- [ ] Reporte de siniestralidad por período, conductor y vehículo

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-090 · Checklist pre-viaje (Vehicle Inspection)
**Como** conductor, **quiero** completar un checklist digital de inspección del vehículo antes de cada viaje, **para** verificar que el vehículo está en condiciones seguras.

**Criterios de aceptación:**
- [ ] Checklist configurable por tipo de vehículo
- [ ] Ítems de inspección: frenos, luces, llantas, nivel de aceite, documentos, carga asegurada
- [ ] El conductor completa el checklist desde la app móvil
- [ ] Si algún ítem falla: alerta al jefe de flota y el viaje no puede iniciarse
- [ ] Registro histórico de inspecciones por vehículo

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-091 · Gestión de mercancías peligrosas (HAZMAT)
**Como** operador especializado, **quiero** gestionar embarques de mercancías peligrosas, **para** cumplir con regulaciones de transporte de HAZMAT.

**Criterios de aceptación:**
- [ ] Clasificación de mercancía según clases HAZMAT (UN/ONU)
- [ ] Validación de que el carrier y vehículo están habilitados para HAZMAT
- [ ] Documentos requeridos: ficha de seguridad (MSDS), declaración de mercancías peligrosas
- [ ] Restricciones de carga: no mezclar ciertas clases HAZMAT
- [ ] Checklist específico de seguridad para embarques HAZMAT

**Estimación:** 8 pts | **Prioridad:** Baja

---

---

# 📊 EP-16 — Analytics & Business Intelligence

## Sprint 19 — Dashboards y KPIs

---

### HU-092 · Dashboard ejecutivo en tiempo real
**Como** gerente general, **quiero** un dashboard con los KPIs más importantes del negocio, **para** tomar decisiones informadas sin necesidad de generar reportes manuales.

**Criterios de aceptación:**
- [ ] KPIs visibles: embarques del día (planificados/en tránsito/entregados), OTD %, costo promedio por km, utilización de flota, top 5 clientes por volumen
- [ ] Filtros: hoy, semana, mes, personalizado
- [ ] Datos actualizados en tiempo real (máx. 5 min de latencia)
- [ ] Gráficas interactivas (click para ver detalle)
- [ ] Exportación del dashboard a PDF

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-093 · KPI de On-Time Delivery (OTD)
**Como** gerente de operaciones, **quiero** medir el porcentaje de entregas a tiempo, **para** evaluar el nivel de servicio y detectar problemas recurrentes.

**Criterios de aceptación:**
- [ ] OTD calculado como: (entregas a tiempo / total entregas) × 100
- [ ] Desglose de OTD por: carrier, ruta, cliente, zona, conductor
- [ ] Tendencia de OTD en el tiempo (gráfica de líneas)
- [ ] Drill-down a embarques específicos que fallaron el OTD con motivo
- [ ] Comparativo vs. período anterior y vs. meta configurada

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-094 · Análisis de costos de flete
**Como** controller, **quiero** analizar los costos de flete en múltiples dimensiones, **para** identificar oportunidades de reducción de costos.

**Criterios de aceptación:**
- [ ] Costo por: km recorrido, kg transportado, embarque, ruta, carrier, cliente, modo de transporte
- [ ] Comparativo de costos entre carriers para la misma ruta
- [ ] Tendencia de costos en el tiempo
- [ ] Impacto del fuel surcharge en el costo total
- [ ] Exportación de datos a Excel para análisis avanzado

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-095 · Análisis de utilización de flota
**Como** gerente de flota, **quiero** analizar la utilización de mi flota, **para** detectar vehículos subutilizados o sobredemandados.

**Criterios de aceptación:**
- [ ] Utilización por vehículo: % de días activos vs. disponibles, % de capacidad usada
- [ ] Vehículos con utilización por debajo del umbral configurado
- [ ] Costo por vehículo vs. ingreso generado
- [ ] Tiempo muerto por vehículo (horas sin asignación)
- [ ] Sugerencia de tamaño óptimo de flota basado en demanda histórica

**Estimación:** 8 pts | **Prioridad:** Media

---

## Sprint 20 — Reportes Avanzados

---

### HU-096 · Constructor de reportes personalizado
**Como** gerente, **quiero** crear mis propios reportes seleccionando campos y filtros, **para** obtener exactamente la información que necesito sin depender del área de sistemas.

**Criterios de aceptación:**
- [ ] Selección de dimensiones (qué mostrar): campos de órdenes, embarques, carriers, clientes, costos
- [ ] Filtros: por fecha, estado, carrier, cliente, zona, modo de transporte
- [ ] Agrupación y suma automática
- [ ] Vista previa del reporte antes de exportar
- [ ] Guardado de reportes favoritos con nombre
- [ ] Exportación a PDF, Excel, CSV

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-097 · Reportes de performance de carriers
**Como** gerente de compras, **quiero** reportes detallados del desempeño de cada carrier, **para** respaldar negociaciones contractuales.

**Criterios de aceptación:**
- [ ] Por carrier: OTD, daños, rechazos, tiempo de respuesta a asignación, costo promedio
- [ ] Período configurable (mensual, trimestral, anual)
- [ ] Comparativo entre carriers competidores
- [ ] Tendencia de indicadores en el tiempo
- [ ] Exportación a PDF para presentar al carrier en reunión de revisión

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-098 · Análisis de emisiones de CO₂ (Sustainability)
**Como** director de sostenibilidad, **quiero** medir las emisiones de CO₂ de mis operaciones de transporte, **para** gestionar la huella de carbono y cumplir compromisos ESG.

**Criterios de aceptación:**
- [ ] Cálculo de CO₂ por embarque basado en: distancia, tipo de vehículo, combustible
- [ ] Factores de emisión por tipo de combustible y modo de transporte configurables
- [ ] Dashboard de emisiones por período, carrier, ruta, cliente
- [ ] Comparativo de emisiones vs. período anterior
- [ ] Reporte de huella de carbono exportable (para reportes ESG)

**Estimación:** 8 pts | **Prioridad:** Baja

---

### HU-099 · Reportes regulatorios y de cumplimiento
**Como** oficial de cumplimiento, **quiero** generar reportes requeridos por autoridades regulatorias, **para** cumplir con obligaciones legales de forma eficiente.

**Criterios de aceptación:**
- [ ] Reportes configurables por país y tipo de regulación
- [ ] Estadísticas de transporte para entes reguladores
- [ ] Reporte de incidentes HAZMAT para autoridades
- [ ] Reporte de HOS para inspecciones
- [ ] Generación con un clic y formato oficial descargable

**Estimación:** 8 pts | **Prioridad:** Baja

---

---

# 🔌 EP-17 — Integraciones & API Pública

## Sprint 21 — API y Conectores ERP

---

### HU-100 · API REST pública documentada
**Como** desarrollador de sistemas del cliente, **quiero** acceder a la API pública de Freiroute, **para** integrar mi sistema con el TMS sin desarrollos complejos.

**Criterios de aceptación:**
- [ ] API REST con autenticación por API Key
- [ ] Endpoints principales: órdenes, embarques, rastreo, documentos, tarifas
- [ ] Documentación interactiva Swagger / OpenAPI 3.0
- [ ] Sandbox de pruebas con datos de muestra
- [ ] Versionado de API (`/api/v1/`, `/api/v2/`)
- [ ] Rate limiting por tenant (configurable)
- [ ] SDKs de ejemplo en Python, JavaScript, C#

**Estimación:** 13 pts | **Prioridad:** Media

---

### HU-101 · Webhooks de eventos
**Como** desarrollador externo, **quiero** recibir notificaciones en tiempo real cuando ocurren eventos en Freiroute, **para** mantener mis sistemas sincronizados sin polling.

**Criterios de aceptación:**
- [ ] Configuración de webhooks por tenant con URL destino
- [ ] Eventos disponibles: orden creada, estado cambiado, entrega completada, excepción, factura emitida
- [ ] Payload en JSON con todos los datos del evento
- [ ] Reintentos automáticos si el endpoint falla (3 reintentos con backoff)
- [ ] Log de webhooks enviados con estado (éxito/fallo) y payload

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-102 · Integración EDI (Electronic Data Interchange)
**Como** cliente enterprise, **quiero** intercambiar datos con Freiroute en formato EDI estándar, **para** integrar con sistemas legacy de mi empresa.

**Criterios de aceptación:**
- [ ] EDI 204 — Motor Carrier Load Tender (recepción de órdenes)
- [ ] EDI 210 — Motor Carrier Freight Invoice (facturas de carriers)
- [ ] EDI 214 — Transportation Carrier Shipment Status (actualizaciones de estado)
- [ ] EDI 990 — Response to a Load Tender (confirmación de asignación)
- [ ] Soporte AS2, SFTP y VAN para intercambio de archivos EDI
- [ ] Mapeo configurable de campos por partner comercial

**Estimación:** 21 pts | **Prioridad:** Baja

---

## Sprint 22 — Integraciones Avanzadas

---

### HU-103 · Integración con sistemas ERP
**Como** gerente de TI, **quiero** integrar Freiroute con el ERP de la empresa, **para** sincronizar órdenes, facturas y datos maestros automáticamente.

**Criterios de aceptación:**
- [ ] Conectores pre-construidos: SAP, Oracle NetSuite, Microsoft Dynamics 365
- [ ] Conector genérico por API REST para otros ERPs
- [ ] Sincronización bidireccional de: clientes, órdenes de compra/venta, facturas
- [ ] Configuración de frecuencia de sincronización (en tiempo real o por lote)
- [ ] Log de sincronización con errores y resolución

**Estimación:** 21 pts | **Prioridad:** Baja

---

### HU-104 · Integración con plataformas de e-Commerce
**Como** e-Commerce manager, **quiero** que los pedidos de mi tienda en línea generen automáticamente órdenes en Freiroute, **para** gestionar la última milla desde el TMS.

**Criterios de aceptación:**
- [ ] Conectores: Shopify, WooCommerce, Magento
- [ ] Creación automática de orden en Freiroute al confirmar pedido en la tienda
- [ ] Actualización automática del estado del pedido en la tienda al entregar
- [ ] Compartir link de rastreo con el cliente final desde la tienda
- [ ] Gestión de devoluciones (reverse logistics) desde el pedido de la tienda

**Estimación:** 13 pts | **Prioridad:** Baja

---

### HU-105 · Integración con telemática GPS
**Como** jefe de flota, **quiero** conectar los dispositivos GPS de la flota con Freiroute, **para** recibir posición y eventos directamente sin captura manual.

**Criterios de aceptación:**
- [ ] Integración con: Samsara, Geotab, Trimble, Calamp, Teltonika
- [ ] Recepción de posición GPS en tiempo real
- [ ] Recepción de eventos: encendido, apagado, velocidad, geofence, temperatura (reefer)
- [ ] Mapeo de dispositivo GPS a vehículo en el sistema
- [ ] Conector genérico MQTT/REST para otros fabricantes de GPS

**Estimación:** 13 pts | **Prioridad:** Media

---

---

# 📱 EP-18 — Mobile App — Conductor

## Sprint 23 — App Conductor (Core)

---

### HU-106 · App móvil del conductor — Login y perfil
**Como** conductor, **quiero** una app móvil para gestionar mis asignaciones, **para** operar sin depender de papeles ni llamadas al dispatcher.

**Criterios de aceptación:**
- [ ] Login con email/contraseña o PIN de 6 dígitos
- [ ] Perfil del conductor con datos personales y documentos vigentes
- [ ] Modo offline: funciona sin conexión y sincroniza al recuperar señal
- [ ] Soporte Android (5.0+) y iOS (13+)
- [ ] Idioma según configuración del dispositivo (ES/EN)

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-107 · Gestión de asignaciones en la app
**Como** conductor, **quiero** ver mis asignaciones del día y próximas, **para** organizar mi jornada de trabajo.

**Criterios de aceptación:**
- [ ] Lista de viajes asignados: hoy y próximos 3 días
- [ ] Detalle del viaje: origen, destino, paradas, carga, instrucciones especiales
- [ ] Documentos adjuntos descargables: manifiesto, carta de porte
- [ ] Confirmación de aceptación del viaje
- [ ] Notificación push al recibir nueva asignación

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-108 · Navegación GPS integrada en app
**Como** conductor, **quiero** navegación turn-by-turn integrada en la app, **para** no necesitar un GPS externo ni saltar entre apps.

**Criterios de aceptación:**
- [ ] Navegación con instrucciones de voz paso a paso
- [ ] Ruta considerando las restricciones de vehículo pesado (puentes, altura, peso)
- [ ] Recálculo automático de ruta si el conductor se desvía
- [ ] Integración con Google Maps / Waze como alternativa
- [ ] Modo oscuro para conducción nocturna

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-109 · Registro de eventos desde la app
**Como** conductor, **quiero** reportar eventos del viaje desde mi app, **para** mantener informado al dispatcher sin llamadas.

**Criterios de aceptación:**
- [ ] Eventos que puede registrar: Salida de origen, Llegada a parada, Salida de parada, En destino, Entregado, Problema en ruta
- [ ] Cada evento captura automáticamente posición GPS y timestamp
- [ ] Campo de comentario opcional por evento
- [ ] Foto adjunta opcional por evento
- [ ] Sincronización inmediata con el sistema central (o en cola offline)

**Estimación:** 5 pts | **Prioridad:** Alta

---

## Sprint 24 — App Conductor (Avanzado)

---

### HU-110 · Captura de POD desde app
**Como** conductor, **quiero** capturar la prueba de entrega digital, **para** confirmar la entrega sin documentos en papel.

**Criterios de aceptación:**
- [ ] Firma digital del receptor en pantalla táctil
- [ ] Foto de la mercancía entregada (cámara del teléfono)
- [ ] Nombre e identificación del receptor
- [ ] El POD se sincroniza inmediatamente con el sistema central
- [ ] Si hay rechazo: registrar motivo y foto de la mercancía rechazada

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-111 · Checklist pre-viaje desde app
**Como** conductor, **quiero** completar el checklist de inspección del vehículo desde la app, **para** documentar el estado del vehículo antes de salir.

**Criterios de aceptación:**
- [ ] Lista configurable de ítems a verificar
- [ ] Respuesta: OK / No OK + foto si hay problema
- [ ] El viaje no puede iniciarse si hay ítems en No OK sin resolución
- [ ] El checklist queda guardado con timestamp y posición GPS

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-112 · Chat en tiempo real conductor–dispatcher
**Como** conductor, **quiero** comunicarme con el dispatcher desde la app, **para** resolver situaciones en ruta sin usar el teléfono.

**Criterios de aceptación:**
- [ ] Chat de texto en tiempo real entre conductor y dispatcher
- [ ] Envío de fotos desde el chat
- [ ] Notificación push al recibir mensaje
- [ ] Historial del chat por viaje
- [ ] Indicador de lectura (visto)

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-113 · Reporte de incidentes desde app
**Como** conductor, **quiero** reportar incidentes desde la app, **para** notificar al dispatcher de forma inmediata con toda la información relevante.

**Criterios de aceptación:**
- [ ] Tipo de incidente: avería, accidente, robo, problema con carga, otro
- [ ] Captura de fotos del incidente
- [ ] Posición GPS automática del incidente
- [ ] Notificación push inmediata al dispatcher y gerente
- [ ] El incidente queda registrado en el sistema central

**Estimación:** 5 pts | **Prioridad:** Media

---

---

# 🔔 EP-19 — Notificaciones & Alertas

## Sprint 25 — Motor de Notificaciones

---

### HU-114 · Motor de notificaciones multicanal
**Como** administrador, **quiero** configurar notificaciones automáticas por múltiples canales, **para** que cada usuario reciba información relevante en el momento correcto.

**Criterios de aceptación:**
- [ ] Canales soportados: email, SMS, Push (app), WhatsApp Business, webhook
- [ ] Plantillas editables por tipo de evento y canal
- [ ] Variables dinámicas en plantillas: {{cliente}}, {{número_embarque}}, {{ETA}}, etc.
- [ ] Configuración por usuario de qué notificaciones recibir y por qué canal
- [ ] Log de todas las notificaciones enviadas con estado (enviado/fallido)

**Estimación:** 13 pts | **Prioridad:** Alta

---

### HU-115 · Alertas operacionales y de excepción
**Como** dispatcher, **quiero** recibir alertas automáticas de excepciones operacionales, **para** reaccionar a tiempo ante problemas.

**Criterios de aceptación:**
- [ ] Alertas configurables: embarque sin asignar X horas antes del pickup, retraso de ETA mayor a Y minutos, vehículo detenido más de Z minutos, documento por vencer, SLA en riesgo
- [ ] Prioridad de alerta: Informativa, Advertencia, Crítica
- [ ] Centro de alertas en el dashboard con bandeja de alertas pendientes
- [ ] Alerta resuelta al tomar acción o marcar manualmente
- [ ] Escalamiento: si no se atiende en X minutos, escala al nivel superior

**Estimación:** 8 pts | **Prioridad:** Alta

---

---

# ⚙️ EP-20 — Configuración & Localización

## Sprint 26 — Configuración Global

---

### HU-116 · Localización y multi-idioma
**Como** administrador de tenant, **quiero** configurar el idioma del sistema, **para** que mis usuarios trabajen en su idioma nativo.

**Criterios de aceptación:**
- [ ] Idiomas disponibles: Español, Inglés, Portugués (Brasil)
- [ ] Cambio de idioma por usuario (no afecta al resto del tenant)
- [ ] Formato de fecha según región: DD/MM/YYYY (ES/PT), MM/DD/YYYY (EN)
- [ ] Formato de números: 1.000,00 (ES) vs 1,000.00 (EN)
- [ ] Zona horaria configurable por usuario y por tenant

**Estimación:** 8 pts | **Prioridad:** Alta

---

### HU-117 · Configuración de monedas y tipos de cambio
**Como** administrador financiero, **quiero** configurar monedas y tipos de cambio, **para** que el sistema opere en múltiples monedas con conversión automática.

**Criterios de aceptación:**
- [ ] Moneda principal del tenant y monedas secundarias
- [ ] Tipo de cambio manual o automático (integración con API de tasas)
- [ ] Actualización diaria de tipos de cambio
- [ ] Conversión automática al generar facturas y reportes
- [ ] Historial de tipos de cambio aplicados

**Estimación:** 5 pts | **Prioridad:** Media

---

### HU-118 · Configuración de impuestos
**Como** contador, **quiero** configurar los impuestos aplicables en el sistema, **para** que las facturas se generen con los impuestos correctos según la normativa local.

**Criterios de aceptación:**
- [ ] Tipos de impuesto configurables: IVA, ISC, retenciones
- [ ] Porcentaje configurable por tipo de impuesto y servicio
- [ ] Exenciones por tipo de cliente o mercancía
- [ ] El impuesto calculado se muestra en el desglose de la factura
- [ ] Vigencia de tasas con historial

**Estimación:** 5 pts | **Prioridad:** Alta

---

### HU-119 · Personalización visual del tenant (White-Label)
**Como** administrador, **quiero** personalizar la apariencia del sistema con mi marca, **para** que mis clientes vean mi identidad corporativa.

**Criterios de aceptación:**
- [ ] Logo del tenant en: barra de navegación, documentos PDF, portal del cliente, emails
- [ ] Color primario y secundario configurables (paleta aplicada en todo el sistema)
- [ ] Nombre del sistema personalizable (white-label completo)
- [ ] Favicon personalizado
- [ ] Dominio personalizado: `tms.miempresa.com` en planes Enterprise

**Estimación:** 8 pts | **Prioridad:** Media

---

### HU-120 · Gestión de backups y retención de datos
**Como** administrador, **quiero** configurar las políticas de backup y retención de datos, **para** cumplir con obligaciones legales y proteger la información.

**Criterios de aceptación:**
- [ ] Backup automático diario de la base de datos por tenant
- [ ] Retención de backups: 30 días (configurable en planes Enterprise)
- [ ] Restauración de backup bajo solicitud formal
- [ ] Configuración de retención de documentos: mínimo 5 años
- [ ] Reporte de espacio de almacenamiento usado vs. límite del plan

**Estimación:** 8 pts | **Prioridad:** Media

---

---

# 📋 RESUMEN EJECUTIVO DEL BACKLOG

## Totales por Épica

| Épica | Módulo | HUs | Pts Estimados | Sprints |
|---|---|---|---|---|
| EP-01 | Infraestructura & Auth | 8 | 63 | SP-01 |
| EP-02 | Admin SaaS & Tenants | 6 | 52 | SP-02 |
| EP-03 | Maestros & Catálogos | 6 | 42 | SP-03 |
| EP-04 | Order Management | 12 | 89 | SP-04–05 |
| EP-05 | Carrier Management | 7 | 60 | SP-06 |
| EP-06 | Shipment Planning | 9 | 85 | SP-07–08 |
| EP-07 | Route Optimization | 6 | 71 | SP-09 |
| EP-08 | Track & Trace | 5 | 50 | SP-10 |
| EP-09 | Document Management | 6 | 42 | SP-11 |
| EP-10 | Freight Audit & Payment | 6 | 58 | SP-12–13 |
| EP-11 | Customer Portal & CRM | 3 | 29 | SP-14 |
| EP-12 | Warehouse & Dock | 3 | 24 | SP-15 |
| EP-13 | Comercio Internacional | 5 | 37 | SP-16 |
| EP-14 | Fleet & Driver | 5 | 37 | SP-17 |
| EP-15 | Compliance & Safety | 4 | 29 | SP-18 |
| EP-16 | Analytics & BI | 8 | 69 | SP-19–20 |
| EP-17 | Integraciones & API | 6 | 89 | SP-21–22 |
| EP-18 | Mobile App Conductor | 8 | 68 | SP-23–24 |
| EP-19 | Notificaciones & Alertas | 2 | 21 | SP-25 |
| EP-20 | Configuración & Localización | 5 | 34 | SP-26 |
| **TOTAL** | | **120 HU** | **~1,049 pts** | **26 Sprints** |

---

## Roadmap de Releases

| Release | Sprints | Módulos Incluidos | Descripción |
|---|---|---|---|
| **MVP v1.0** | SP-01 a SP-11 | Auth, Admin, Maestros, Órdenes, Carriers, Planificación, Ruteo, Rastreo, Documentos | Sistema operativo core |
| **v1.5** | SP-12 a SP-16 | Facturación, Portal Cliente, Almacén, Internacional | Monetización y expansión |
| **v2.0** | SP-17 a SP-20 | Flota, Compliance, Analytics, BI | Gestión avanzada y datos |
| **v2.5** | SP-21 a SP-26 | API, Integraciones, Mobile App, Notificaciones, Config | Ecosistema completo |

---

## Velocidad de Equipo Sugerida

| Perfil del Equipo | Velocidad/Sprint | Duración Sprint | Tiempo Estimado Total |
|---|---|---|---|
| Equipo pequeño (2 devs) | 20–30 pts | 2 semanas | ~36 meses |
| Equipo mediano (4 devs) | 50–60 pts | 2 semanas | ~18 meses |
| Equipo grande (6+ devs) | 80–100 pts | 2 semanas | ~12 meses |

---

*Documento generado para el proyecto Freiroute TMS SaaS Multi-Tenant*  
*Versión 1.0 — 2026*  
*Referencia: Oracle TMS · SAP Transportation Management · MercuryGate TMS · BluJay Solutions · Trimble TMS*

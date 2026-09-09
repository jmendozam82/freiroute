namespace Freiroute.Utility.Constants;

/// <summary>
/// Acciones estándar registradas en 'auditoria_actividad.accion' (HU-008 CA-01).
/// El log es inmutable y registra automáticamente cada operación del sistema.
/// Sprint 2: se agregan acciones de OAuth, impersonación, facturación y reactivación.
/// </summary>
public static class AccionAuditoria
{
    // ── Auth (Sprint 1) ──────────────────────────────────────────
    public const string LOGIN = "LOGIN";
    public const string LOGOUT = "LOGOUT";
    public const string LOGIN_FAILED = "LOGIN_FAILED";

    // ── CRUD genérico (Sprint 1) ─────────────────────────────────
    public const string CREATE = "CREATE";
    public const string UPDATE = "UPDATE";
    public const string DEACTIVATE = "DEACTIVATE";
    public const string EXPORT = "EXPORT";
    public const string CAMBIO_ESTADO = "CAMBIO_ESTADO";

    // ── Auth OAuth (Sprint 2 — HU-004) ───────────────────────────
    public const string LOGIN_OAUTH = "LOGIN_OAUTH";

    // ── Super Admin (Sprint 2 — HU-009) ──────────────────────────
    public const string IMPERSONACION = "IMPERSONACION";

    // ── Facturación (Sprint 2 — HU-011) ──────────────────────────
    public const string CAMBIAR_PLAN = "CAMBIAR_PLAN";
    public const string REGISTRAR_PAGO = "REGISTRAR_PAGO";

    // ── Reactivación (Sprint 2 — HU-013) ─────────────────────────
    public const string REACTIVAR = "REACTIVAR";

    // ── Órdenes avanzadas (Sprint 5 — HU-028 a HU-032) ───────────
    public const string VINCULAR_PO           = "VINCULAR_PO";
    public const string AUTO_PRIORIDAD        = "AUTO_PRIORIDAD";
    public const string CAMBIO_PRIORIDAD      = "CAMBIO_PRIORIDAD";
    public const string RECHAZO_ENTREGA       = "RECHAZO_ENTREGA";
    public const string CREAR_REENTREGA       = "CREAR_REENTREGA";
    public const string CREAR_RECLAMO         = "CREAR_RECLAMO";
    public const string CAMBIO_ESTADO_RECLAMO = "CAMBIO_ESTADO_RECLAMO";
}
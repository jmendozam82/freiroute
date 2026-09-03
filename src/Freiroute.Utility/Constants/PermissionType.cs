namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de permiso del sistema (HU-006 CA-03, ADR-009).
/// Solo existen READ, CREATE y UPDATE — NO existe DELETE (regla AGENTS.md).
/// Los claims JWT se serializan como "modulo:read|create|update".
/// </summary>
public enum PermissionType
{
    Read,
    Create,
    Update
}
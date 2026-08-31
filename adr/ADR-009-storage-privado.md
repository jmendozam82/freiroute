# ADR-009: Supabase Storage Privado con URLs de Firma Temporal

| Campo | Valor |
|---|---|
| **ID** | ADR-009 |
| **Título** | Uso de buckets de almacenamiento privados y URLs firmadas de corta duración frente a URLs públicas |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-27 |
| **Decidido por** | Oficial de Seguridad (CISO) + Arquitecto de software |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

El sistema SaaS (Vittal) procesa información médica y administrativa sensible. Los usuarios pueden adjuntar archivos, como resultados de laboratorio en PDF, imágenes médicas y documentos de identidad.

Supabase Storage ofrece dos tipos de buckets para almacenar objetos:
1. **Buckets Públicos:** Cualquier archivo en el bucket tiene una URL pública estática (ej: `https://[project-ref].supabase.co/storage/v1/object/public/bucket/file.pdf`).
2. **Buckets Privados:** Los archivos no son accesibles públicamente por defecto. Requieren autenticación (token JWT) para acceder vía API, o la generación de una URL firmada (Signed URL) válida por un tiempo limitado.

Se debe decidir la política de almacenamiento predeterminada para el framework.

---

## Decisión

**Todos los buckets de Supabase Storage que contengan datos de negocio de los tenants (como expedientes, documentos, anexos) DEBEN ser configurados como Privados. El acceso a estos archivos se realizará exclusivamente mediante la generación de Signed URLs (URLs firmadas de corta duración) desde el backend (API).**

Los buckets públicos se restringirán estrictamente a recursos no confidenciales y estáticos que requieran almacenamiento en caché en CDN, como logos de clínicas, recursos estáticos del sistema o avatares de usuario genéricos (no identificables).

---

## Alternativas Evaluadas

### Opción A: Buckets Públicos con UUIDs inescrutables (RECHAZADA)

Se usan buckets públicos pero el nombre del archivo se reemplaza por un UUID aleatorio.

**Ventajas:**
- Simplicidad: se guarda la URL pública generada directamente en la base de datos y se sirve al frontend sin procesamiento extra.
- Menos carga computacional y de red en el backend.

**Desventajas que motivaron su rechazo:**
- Seguridad por oscuridad (Security through obscurity). Un atacante no puede adivinar el nombre del archivo, pero si la URL se filtra (ej. a través de un proxy corporativo, historial del navegador, o log compartido), el archivo médico queda expuesto permanentemente en internet.
- Incompatible con regulaciones como HIPAA o GDPR, que exigen control de acceso verificable a la información de salud protegida (PHI).
- No se puede auditar quién accedió al archivo usando el nivel de aplicación.

### Opción B: Buckets Privados con Signed URLs (ELEGIDA) ✅

Los archivos se guardan en un bucket privado. Cuando un usuario autenticado y autorizado (validado en la BLL) necesita ver un archivo, el backend solicita a Supabase Storage una URL firmada temporal (ej. válida por 60 segundos) y se la entrega al frontend.

**Ventajas:**
- Seguridad demostrable: incluso si la URL se filtra, expirará casi de inmediato, limitando la ventana de exposición.
- Control de autorización profundo: El backend evalúa si el usuario tiene permiso (RLS o BLL) antes de emitir el ticket de acceso temporal.
- Cumple con estrictas normativas de privacidad de datos.

**Desventajas aceptadas:**
- Carga adicional: Cada vez que un registro se carga en el frontend con documentos adjuntos, el backend debe procesar una solicitud extra a la API de Supabase para generar las URLs firmadas.
- Latencia: Generar URLs en tiempo real para listas con muchos adjuntos puede añadir milisegundos notables al tiempo de respuesta del request (mitigable mediante peticiones en lotes de URLs a Supabase Storage).
- Las URLs generadas no son cacheables a largo plazo en CDNs perimetrales por su naturaleza efímera.

---

## Consecuencias

### Positivas
- Prevención total contra la exposición involuntaria a largo plazo de expedientes médicos e información de identidad personal (PII).
- El esquema de nombres en los buckets (`/tenant_id/entidad_id/archivo_uuid.pdf`) puede usarse de forma segura y transparente sin riesgo de enumeración pública.

### Negativas / Trade-offs aceptados
- Desarrollo más tedioso: el modelo DTO no almacena una URL final estática. El servicio BLL debe transformar el "path" guardado en base de datos en una "Signed URL" justo antes de enviar el ResponseDto al cliente.

### Patrón de Implementación

```csharp
// Guardar el path interno (relativo) en la BD, NO la URL completa
var filePath = $"{tenantId}/expedientes/{pacienteId}/{fileUuid}.pdf";

// Al leer, generar la URL efímera (válida por 60 seg)
public async Task<string> GetSecureFileUrlAsync(string filePath)
{
    // Generar Signed URL usando el cliente de Supabase C#
    var url = await _supabase.Storage
                             .From("documentos_privados")
                             .CreateSignedUrl(filePath, 60); 
    return url;
}
```

---

## Referencias

- [Supabase Storage - Access Control](https://supabase.com/docs/guides/storage/access-control)
- [HIPAA Security Rule - Technical Safeguards](https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html)

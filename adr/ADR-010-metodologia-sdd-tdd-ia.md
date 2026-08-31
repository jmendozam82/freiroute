# ADR-010: Metodología Convergente 2026 (SDD + TDD + SCRUM-IA)

| Campo | Valor |
|---|---|
| **ID** | ADR-010 |
| **Título** | Adopción de Spec-Driven Development, Test-Driven Development y SCRUM con Agentes IA |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-02-01 |
| **Decidido por** | VP de Ingeniería + Tech Lead |
| **Revisado en** | Vittal Sprint 0 — Pilares Metodológicos |

---

## Contexto

El auge de los agentes de codificación impulsados por Inteligencia Artificial (Claude Code, Cursor, Copilot) exige una reevaluación fundamental de las metodologías de desarrollo. El desarrollo de software "clásico", donde el humano escribe el código imperativo basado en requerimientos vagos y delega el testing al final del ciclo (o a un equipo de QA separado), se ha vuelto ineficiente cuando un modelo de IA puede generar miles de líneas de código en minutos.

El riesgo principal del uso desestructurado de agentes de IA es la acumulación masiva de deuda técnica ("código espagueti" generado por IA) debido a la falta de restricciones arquitectónicas claras antes de la generación de código.

Es necesario definir la metodología estándar para orquestar la colaboración humano-IA en el framework.

---

## Decisión

**Todo proyecto derivado de este framework adoptará estrictamente la metodología de "Los 4 Pilares Convergentes de 2026":**

1. **SDD (Spec-Driven Development):** Escribir una especificación formal y commitearla antes de generar cualquier código.
2. **TDD (Test-Driven Development):** El agente (QA/Dev) debe escribir los tests unitarios o de integración que fallan como representación ejecutable de la especificación.
3. **SCRUM-IA:** Los agentes son orquestados como desarrolladores especializados dentro del Sprint Board. El humano actúa como Product Owner, Revisor y Arquitecto Jefe.
4. **IaC+BaaS:** (Tratado en ADR-003, uso intensivo de Supabase y automatización CI/CD).

---

## Alternativas Evaluadas

### Opción A: Desarrollo Clásico (Code-First) asistido por Copiloto (RECHAZADA)

El desarrollador humano comienza a escribir clases y métodos, usando autocompletado y chat en línea para ayudar con fragmentos de código.

**Desventajas que motivaron su rechazo:**
- Altamente dependiente de la memoria a corto plazo del humano y del contexto limitado de la ventana de chat.
- Propensión a errores arquitectónicos: el agente resuelve el problema local sin entender la arquitectura global (ej. bypass del RLS o capas MVC).
- Baja reproducibilidad: si el contexto se pierde, es difícil instruir al agente para continuar el trabajo coherentemente.

### Opción B: Prompt Engineering Ad-Hoc / Zero-Shot Coding (RECHAZADA)

Pedir al agente: *"Crea un módulo de pacientes completo con frontend y backend"* en un solo prompt sin planificación previa.

**Desventajas que motivaron su rechazo:**
- Genera soluciones monolíticas, acopladas y difíciles de testear.
- Ignora silenciosamente los requerimientos no funcionales (como el filtro por `tenant_id` obligatorio).
- Incompatible con el ciclo de vida de Pull Requests y CI/CD riguroso.

### Opción C: Metodología Estructurada SDD + TDD (ELEGIDA) ✅

La orquestación del agente se restringe mediante artefactos intermedios verificables.

Flujo:
1. Humano y Agente colaboran para escribir `spec.md`. (Punto de control humano).
2. Agente (Rol QA) lee `spec.md` y genera tests que fallan (TDD). (Punto de verificación).
3. Agente (Rol Dev) lee `spec.md`, lee tests que fallan, y escribe la implementación mínima (Entity, DTO, BLL, DAL).
4. Ejecución de tests: Se repite el ciclo hasta que todos los tests pasen verde.
5. Humano revisa el Pull Request final.

**Ventajas:**
- Las "alucinaciones" de la IA son capturadas instantáneamente por los tests que fallan (TDD actúa como arnés de seguridad).
- Forzar la creación del `spec.md` asegura que el agente tenga el contexto completo antes de codificar.
- Permite escalar: un agente puede generar specs, otro escribir tests, y otro implementar código en paralelo (Agent Teams).

**Desventajas aceptadas:**
- Aumento de la fricción percibida al inicio (escribir un documento antes de ver "código funcionando").
- Requiere disciplina para no saltarse el paso del spec en "tareas rápidas".

---

## Consecuencias

### Positivas
- Código altamente predecible y alineado con los estándares definidos en `AGENTS.md` (La Constitución).
- Base de código que se auto-documenta y posee alta cobertura de testing desde el día 1.
- Velocidad sostenida a largo plazo: al reducir la deuda técnica inducida por la IA, la velocidad del Sprint 10 es tan alta como la del Sprint 1.

### Negativas / Trade-offs aceptados
- Requiere un cambio cultural profundo en los desarrolladores humanos, pasando del rol de "albañil del código" al rol de "arquitecto y orquestador".
- Las iteraciones (loops) locales de prueba y error del agente consumen más tokens (y presupuesto de API) al requerir compilación y ejecución constante de la suite de tests en background.

---

## Referencias

- Framework `docs/framework/lifecycle.md` (Documentación central de esta metodología).
- [The Rise of AI Engineering (2025-2026 trends)]

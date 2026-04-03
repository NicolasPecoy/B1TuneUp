# ?? Documentación de B1TuneUp

## Bienvenido a la Documentación Oficial de B1TuneUp

Este directorio contiene **todo el material actualizado (abril 2026)** para usar, configurar y extender B1TuneUp. Tras la incorporación de los nuevos **estudios WPF** y del runtime `InvokeHandler`, cada documento fue reescrito con ejemplos paso a paso.

---

## ?? Estructura principal

| Documento | Perfil | Contenido destacado |
|-----------|--------|---------------------|
| [**DOCUMENTACION_COMPLETA_B1TUNEUP.md**](./DOCUMENTACION_COMPLETA_B1TUNEUP.md) | Consultores / Implementadores | 9 capítulos: introducción, arquitectura, instalación, mapa de estudios WPF, InvokeHandler, guías paso a paso, referencia de macros moderna, FAQ, soporte. Incluye escenarios detallados para cada estudio. |
| [**GUIA_RAPIDA_B1TUNEUP.md**](./GUIA_RAPIDA_B1TUNEUP.md) | Usuarios operativos / Soporte | Arranque exprés, tabla de estudios WPF, ejemplos rápidos (integración, botones con InvokeHandler, validaciones, dashboards, emails), comandos esenciales y troubleshooting. |
| [**GUIA_TECNICA_B1TUNEUP.md**](./GUIA_TECNICA_B1TUNEUP.md) | Desarrolladores | Arquitectura del repositorio, patrones, servicios, pruebas e integración con SAP SDK. (Se mantiene vigente; consulta esta guía cuando necesites extender código). |

---

## ?? ¿Por dónde empezar?

- **Usuario final / consultor funcional**: Guía Rápida ? secciones 1 a 3 y luego Documentación Completa cap. 4 (Estudios WPF).
- **Soporte**: Guía Rápida ? Troubleshooting (sección 7) + Documentación Completa cap. 8 (FAQ).
- **Desarrollador**: Guía Técnica + Documentación Completa cap. 5 (InvokeHandler) y cap. 7 (Referencia de macros actualizada).
- **Implementador**: Documentación Completa caps. 2 y 3 (arquitectura/instalación) y escenarios del cap. 6.

---

## ??? Tabla de contenidos actualizada

### DOCUMENTACION_COMPLETA_B1TUNEUP.md

```
1. Introducción (visión general, objetivos y beneficios)
2. Arquitectura (diagrama WPF?SAP, componentes, flujos)
3. Instalación y configuración paso a paso
4. Estudios WPF (mapa general + 15 sub-secciones con procedimientos)
5. InvokeHandler y extensibilidad avanzada
6. Guías paso a paso (integración, UI, validaciones, dashboards, auditoría)
7. Referencia de macros (nuevos comandos: InvokeHandler, RunIntegration, ShowStudio)
8. Preguntas frecuentes
9. Soporte, capacitación y roadmap
```

### GUIA_RAPIDA_B1TUNEUP.md

```
1. Arranque exprés (checklist de 5 minutos)
2. Mapa de estudios WPF (tabla comparativa)
3. Ejemplos rápidos (integración REST, botón con InvokeHandler, validaciones, dashboards, emails)
4. Primera macro moderna (InvokeHandler incluido)
5. Referencias mínimas de comandos
6. Plantilla de handler
7. Troubleshooting rápido
8. Checklist previo a producción
```

### GUIA_TECNICA_B1TUNEUP.md

*(sin cambios estructurales en esta iteración; sigue cubriendo arquitectura interna, servicios y patrones de desarrollo).* 

---

## ?? Cómo buscar información

- **Quiero configurar un estudio** ? Documentación Completa, cap. 4 (cada sección incluye flujo de trabajo en 5 pasos).
- **Necesito un ejemplo inmediato** ? Guía Rápida, sección 3.
- **Voy a escribir un handler personalizado** ? Documentación Completa cap. 5 + Guía Rápida sección 6.
- **Tengo un error en SAP** ? Guía Rápida sección 7 + Documentación Completa cap. 8.

Usa `Ctrl+F` dentro de los `.md` para buscar `Integration Studio`, `InvokeHandler`, `Automation Dashboard`, etc.

---

## ?? Métricas

| Métrica | Valor |
|---------|-------|
| Documentos actualizados | 2 (Documentación completa + Guía rápida) |
| Estudios WPF documentados | 15 |
| Escenarios paso a paso | 10+ |
| Nuevos comandos descritos | 3 (`InvokeHandler`, `RunIntegration`, `ShowStudio`) |

---

## ?? Feedback

- Email: `docs@b1tuneup.com`
- Foro: `forum.b1tuneup.com`
- Issues: `github.com/b1tuneup/B1TuneUp/issues`

Incluye versión de B1TuneUp y referencia del documento cuando reportes sugerencias.

---

**Última actualización:** 3 de abril de 2026 – Equipo de Documentación B1TuneUp.

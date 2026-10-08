# Documentación del proyecto

Índice de la documentación del E-Commerce con microservicios. Para ejecutar el proyecto, ver el [README principal](../README.md).

| Carpeta | Documento | Para qué sirve |
|---|---|---|
| [`consignas/`](consignas/) | [TP_Microservicios_ECommerce_v7.md](consignas/TP_Microservicios_ECommerce_v7.md) | El enunciado de la cátedra en Markdown, para leerlo desde GitHub |
| | [TP_Microservicios_ECommerce_v7.docx](consignas/TP_Microservicios_ECommerce_v7.docx) | El documento original; ante cualquier diferencia, vale este |
| [`planificacion/`](planificacion/) | [plan-de-desarrollo.md](planificacion/plan-de-desarrollo.md) | La guía de trabajo: estado actual, estructura, convenciones de código, reparto de tareas, decisiones de diseño (D-01 a D-32) y etapas |
| [`arquitectura/`](arquitectura/) | [arquitectura.md](arquitectura/arquitectura.md) | Diagramas del sistema, de capas, de clases y de secuencia |
| [`repasos/`](repasos/) | [repaso-general.md](repasos/repaso-general.md) | Vista de conjunto: línea de tiempo, planificación, trabajo en equipo, arquitectura, revisiones de código y próximos pasos |
| | [repaso-products-api.md](repasos/repaso-products-api.md) | Cómo se construyó Products.API, que es la plantilla de todas las APIs |
| | [repaso-cart-api.md](repasos/repaso-cart-api.md) | Cómo se construyó Cart.API, el primer servicio que consume a otro |
| | [repaso-users-api.md](repasos/repaso-users-api.md) | Cómo se construyó Users.API: contraseñas, bloqueo y el contrato del que dependen otros servicios |
| | [repaso-notifications-api.md](repasos/repaso-notifications-api.md) | Cómo se construyó Notifications.API: envío simulado y cliente de Users |
| `capturas/` | *(Etapa 10)* | Capturas de Swagger UI con ejemplos de error, para el entregable |

## Repasos por API

Cada API tiene su propio repaso, que escribe su responsable al terminarla (convención "Documentación por API" del [plan](planificacion/plan-de-desarrollo.md#convenciones-de-código)):

| API | Responsable | Repaso |
|---|---|---|
| Products.API | Thomas | ✅ [repaso-products-api.md](repasos/repaso-products-api.md) |
| Cart.API | Thomas | ✅ [repaso-cart-api.md](repasos/repaso-cart-api.md) |
| Users.API | Juan Pablo | ✅ [repaso-users-api.md](repasos/repaso-users-api.md) |
| Orders.API | Juan Pablo | ⬜ `repaso-orders-api.md` |
| Notifications.API | Juan Pablo | ✅ [repaso-notifications-api.md](repasos/repaso-notifications-api.md) |

Cada repaso cubre: qué se construyó, las decisiones con su porqué, cómo se testeó, el recorrido completo de un request, el mapa de archivos y preguntas probables de la defensa.

## Por dónde empezar

- **Para entender el proyecto:** [repaso general](repasos/repaso-general.md), secciones 1 a 5.
- **Para entender el código:** el [repaso de Products.API](repasos/repaso-products-api.md), sobre todo la sección 7 (el recorrido de un request), y después el de la API que interese.
- **Para trabajar:** el [plan de desarrollo](planificacion/plan-de-desarrollo.md): estado, convenciones y tareas de cada etapa.
- **Para preparar la defensa:** la última sección de cada repaso tiene preguntas probables con sus respuestas.

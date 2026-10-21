# Guion de la demo

Presentación grupal de 15 a 20 minutos (sección 9.3 del enunciado): invocar endpoints exitosos y de error desde Swagger, mostrar logs y health checks. Cada integrante tiene que poder explicar cualquier parte del código: para eso está la [revisión cruzada](revision-cruzada.md).

El orden sigue los criterios de evaluación (sección 8), de mayor a menor peso.

| # | Bloque | Quién | Minutos | Criterio que muestra |
|---|---|---|---|---|
| 1 | Arquitectura | Thomas | 2 | Diseño del código |
| 2 | Flujo de compra de punta a punta | Juan Pablo | 4 | Endpoints y códigos HTTP (35 %) |
| 3 | Errores del catálogo | Thomas | 3 | Códigos de error (15 %) y `IExceptionHandler` (15 %) |
| 4 | Swagger | Juan Pablo | 2 | Documentación Swagger (15 %) |
| 5 | Logs y Correlation ID | Thomas | 3 | Serilog y Correlation ID (12 %) |
| 6 | Health checks y caída de un servicio | Juan Pablo | 3 | Health checks (8 %) |
| 7 | Cómo lo verificamos | Thomas | 1 | Diseño del código y README |
| | **Total** | | **18** | Quedan 2 minutos de margen para preguntas durante la demo |

---

## Antes de empezar (10 minutos antes)

1. Correr la prueba integral, con los servicios **apagados**: tiene que terminar en "59 de 59 chequeos correctos".

   ```bash
   python3 scripts/verify-system.py
   ```

2. Levantar los cinco servicios (así arrancan con los datos semilla intactos):

   ```bash
   ./scripts/start-all.sh
   ```

3. Dejar abiertos:
   - el navegador con una pestaña de Swagger por servicio (`http://localhost:5001/swagger` a `5005`), en ese orden;
   - la terminal de `start-all.sh` a la vista, con letra grande: ahí salen los logs de los cinco;
   - una segunda terminal en la raíz del repo, para apagar un servicio en el bloque 6 y buscar en los logs;
   - el README en GitHub, en la sección "Arquitectura".

**Plan B:** si algo falla en vivo, las capturas de [docs/capturas/](../capturas/README.md) muestran una respuesta de error real de cada servicio.

**Datos para tener a mano** (también están en el README, "Datos de prueba"):

| Dato | Valor |
|---|---|
| María (activa) | `a1b2c3d4-0000-0000-0000-111122223333` · `maria@email.com` |
| Juan (bloqueado por intentos) · Carlos (bloqueado manualmente) | `juan@email.com` · `carlos@email.com` |
| Contraseña de todos | `MiPassword123!` |
| Notebook (stock 10) | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| Taladro (stock 2) | `b2c3d4e5-0000-0000-0000-000000000005` |
| Orden semilla (Pendiente, con la Notebook) | `f1e2d3c4-0000-0000-0000-aabbccddeeff` |

---

## 1. Arquitectura — Thomas (2 min)

Mostrar el diagrama del README.

- Cinco microservicios, cada uno con su puerto, sus datos y su proyecto de tests, en una sola solución.
- Se comunican solo por HTTP con `IHttpClientFactory`: Cart y Orders consultan a Products; Orders y Notifications, a Users; Products consulta a Orders antes de borrar.
- Dentro de cada API: Controller → Service → Repository / Clients, siempre a través de interfaces. Persistencia en memoria hasta que llegue la librería de la cátedra: cuando llegue, solo cambia la implementación del repositorio.

> **Frase clave:** "Cada servicio es dueño de sus datos. Si Cart necesita el stock, se lo pregunta a Products; no lo lee de su base."

## 2. Flujo de compra de punta a punta — Juan Pablo (4 min)

Todo desde Swagger, con **Try it out**:

1. **Users** · `POST /api/users/login` con María → **200** con sus datos.
2. **Cart** · `POST /api/cart/{userId}/items` con María y 1 Notebook → **200**: Cart le preguntó a Products si existe y si hay stock.
3. **Orders** · `POST /api/orders` con María y 1 Notebook → **201**, con el precio tomado de Products y el total calculado.
4. **Orders** · `PUT /api/orders/{id}/status` → `Confirmada` → **200**.
5. **Notifications** · `POST /api/notifications/send` a María, tipo `Email` → **201**, estado `Enviada`.
6. **Notifications** · `GET /api/notifications/{userId}` de María → **200** con la lista.

> **Frase clave:** "Cada endpoint devuelve solo los status de su contrato: el POST de una orden da 201, el del carrito 200, como dice el enunciado (D-12)."

## 3. Errores del catálogo — Thomas (3 min)

Elegir uno por servicio, mostrando que el body siempre tiene el mismo formato:

| Servicio | Request | Respuesta |
|---|---|---|
| Products | `GET /api/products/99` | 404 · PRD-001 (el ejemplo del enunciado) |
| Products | `DELETE` de la Notebook | 409 · PRD-004: la orden semilla está Pendiente |
| Users | login de Juan con la contraseña correcta | 403 · USR-004 |
| Orders | `POST /api/orders` con 5 taladros | 422 · ORD-005 |
| Orders | `PUT .../status` → `Pendiente` sobre la orden confirmada en el bloque 2 | 409 · ORD-006 |
| Cart | `POST` con cantidad 0 | 400 · CRT-004, de la validación automática |

Después, mostrar en el código **por qué** todos tienen el mismo formato:

- el servicio lanza una excepción de dominio con su `errorCode` (`BusinessRuleException`, `NotFoundException`, `ValidationException`);
- `UseExceptionHandler` la pasa por los cuatro `IExceptionHandler`, del más específico al global;
- todos arman la respuesta con `ErrorResponseWriter`, el único lugar que conoce el formato. El controller no tiene ni un `try/catch`.

> **Frase clave:** "No hay middleware propio para errores, como pide la sección 5.2: es `UseExceptionHandler` con un `IExceptionHandler` por tipo de excepción."

## 4. Swagger — Juan Pablo (2 min)

En el Swagger de Orders, expandir `POST /api/orders`:

- descripción en español, tomada de los XML comments, y los endpoints agrupados por tag;
- ejemplo de request y de respuesta 201;
- **un ejemplo por cada código de error** (ORD-002 a ORD-007), con el formato completo. Los genera el mismo `ErrorResponseWriter` que arma las respuestas reales, así que no pueden quedar desactualizados (D-23).

## 5. Logs y Correlation ID — Thomas (3 min)

1. En la terminal de `start-all.sh`, mostrar una línea: hora, nivel, servicio, **Correlation ID**, endpoint y mensaje.
2. Repetir el `POST /api/orders` del bloque 2. En Swagger, copiar el header **`x-correlation-id`** de la respuesta.
3. En la segunda terminal, buscar ese ID en los archivos de log:

   ```bash
   grep -h "<id copiado>" src/*/logs/*.json
   ```

   Aparece en **Orders, Users y Products**: el mismo ID cruzó los tres servicios.
4. Mostrar una línea del archivo JSON: `Servicio`, `Endpoint`, `CorrelationId` y `ErrorCode` son propiedades, no texto suelto. Los errores de negocio se registran como `Warning` y los inesperados como `Error`.

> **Frase clave:** "Un `DelegatingHandler` agrega el header a cada llamada saliente, sin que los clientes HTTP sepan que existe."

## 6. Health checks y caída de un servicio — Juan Pablo (3 min)

1. `GET http://localhost:5003/health/ready` → **Healthy**, con `persistencia`, `Users.API` y `Products.API`.
2. En la segunda terminal, apagar solo Users:

   ```bash
   pkill -f "net10.0/Users.API"
   ```

3. `/health/ready` de Orders → **200 Degraded**, con `Users.API` en Degraded. `/health/live` sigue Healthy.
4. Crear una orden → **500 · ORD-007**. Ver la orden semilla → **200**: lo que no necesita a Users sigue funcionando.
5. Volver a levantar Users:

   ```bash
   dotnet run --project src/Users.API --launch-profile http --no-build
   ```

> **Frase clave:** "`live` dice si el proceso responde; `ready`, si puede atender todo. Con una dependencia caída el servicio queda `Degraded` y no `Unhealthy`, porque sigue atendiendo lo que no la necesita."

## 7. Cómo lo verificamos — Thomas (1 min)

- **493 tests** (unitarios y de integración) escritos con TDD, que corren en GitHub Actions en cada push.
- `scripts/verify-system.py`: la prueba integral que corrimos antes de empezar, con los 28 códigos del catálogo, los health checks, el Correlation ID entre servicios y los logs en archivo. 59 chequeos.

---

## Preguntas probables

Las de cada servicio están en la última sección de cada repaso, y las del proyecto en general en la [sección 15 del repaso general](../repasos/repaso-general.md#15-preguntas-generales-de-la-defensa). La [revisión cruzada](revision-cruzada.md) indica cuáles preparar.

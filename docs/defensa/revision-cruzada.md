# Revisión cruzada

La sección 9.3 del enunciado pide que **cada integrante pueda explicar cualquier parte del código**. Para prepararlo, cada uno explica en voz alta los servicios del otro, con el código abierto, y el otro corrige.

| Quién explica | Qué | Material |
|---|---|---|
| **Thomas** | Users.API, Orders.API y Notifications.API | [Repaso de Users](../repasos/repaso-users-api.md), [de Orders](../repasos/repaso-orders-api.md) y [de Notifications](../repasos/repaso-notifications-api.md) |
| **Juan Pablo** | Products.API (la plantilla) y Cart.API | [Repaso de Products](../repasos/repaso-products-api.md) y [de Cart](../repasos/repaso-cart-api.md) |

Para cada servicio: primero el **recorrido completo de un request** (sección 7 u 8 de su repaso), y después las preguntas de abajo. La revisión está completa cuando cada uno puede responderlas sin leer.

---

## Thomas explica los servicios de Juan Pablo

### Users.API

- ¿Cómo se guardan las contraseñas y por qué nunca salen en una respuesta?
- Recorrer tres logins fallidos de un usuario nuevo: ¿qué responde cada uno y por qué el tercero da 401 y no 403 (D-09)?
- ¿Cómo distingue USR-004 de USR-005 si el modelo no tiene un campo "motivo de bloqueo" (D-08)?
- ¿Por qué el email es la clave del `ConcurrentDictionary` del repositorio?
- ¿Por qué existe `GET /api/users/{id}` si el enunciado no lo pide (D-06), y quiénes lo usan?

### Orders.API

- La máquina de estados: ¿qué transiciones permite `OrderStatusTransitions` y qué responde una inválida (ORD-006)?
- Crear una orden: ¿en qué orden se consulta a Users y a Products, y qué código sale si falla cada uno (ORD-003, ORD-004, ORD-005, ORD-007)?
- ¿Por qué el precio se toma de Products y no del request?
- Si un producto aparece dos veces en la misma orden, ¿qué pasa con el stock (D-34)?
- ¿Por qué `GET /api/orders` con un filtro inválido devuelve `[]` y no un error (D-37)?
- ¿Para qué está la orden semilla y qué cambia en Products gracias a ella?

### Notifications.API

- ¿Qué hace `INotificationSender` y por qué es una interfaz si el envío es simulado?
- ¿Por qué `SendNotificationRequest.UsuarioId` es `Guid?` y no `Guid`?
- ¿Por qué el `GET` no consulta a Users.API (D-30)?
- ¿Qué pasa si se envía una notificación a un usuario bloqueado (D-31)?

---

## Juan Pablo explica los servicios de Thomas

### Products.API (la plantilla de todas las APIs)

- El pipeline de `Program.cs`: ¿por qué el Correlation ID va primero y `UseExceptionHandler` después del log de requests?
- Recorrer `GET /api/products/99` desde que llega hasta que sale el JSON de PRD-001: ¿qué clase interviene en cada paso?
- ¿Por qué el id de la ruta es `string` y no `{id:guid}` (D-17)?
- ¿Cómo arma PRD-002 el mensaje "A; B; C." con todos los errores de validación (D-19)?
- ¿Por qué Serilog no usa `Log.Logger` (D-21) y qué bug encontramos con un test?
- ¿Cómo se generan los ejemplos de error de Swagger (D-23)?
- PRD-004: ¿qué consulta `OrdersClient` y qué pasa si Orders está caído (D-39)?

### Cart.API

- ¿Por qué la clase se llama `ShoppingCart` y no `Cart`?
- Agregar un producto que ya está en el carrito: ¿qué cantidad se valida contra el stock (D-13)? ¿Y en el `PUT`?
- ¿Por qué un 404 de Products es CRT-002 y un 500 de Products es CRT-005 (D-28)?
- ¿Qué diferencia hay entre vaciar el carrito y quitar el último producto (D-27)?
- ¿Cómo se testea Cart sin levantar Products, y por qué `FakeProductsApi` reemplaza la red y no el cliente?

---

## Preguntas que cualquiera de los dos tiene que poder responder

- ¿Qué es el Correlation ID, cómo se genera, cómo se valida (D-20) y cómo viaja a otro servicio?
- ¿Por qué `/health/ready` informa `Degraded` y no `Unhealthy` cuando se cae una dependencia (D-24)?
- ¿Qué cambia cuando llegue la librería de persistencia de la cátedra?
- ¿Por qué no hay una librería compartida entre los servicios, si el código de la plantilla se repite (D-05)?
- ¿Qué es TDD y en qué los ayudó? Dar un ejemplo concreto de un bug que encontró un test.

Más preguntas, con sus respuestas: sección 15 del [repaso general](../repasos/repaso-general.md#15-preguntas-generales-de-la-defensa) y la última sección de cada repaso.

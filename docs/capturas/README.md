# Capturas de Swagger UI

Respuestas de error reales de cada servicio, con `errorCode` y `errorMessage` (sección 9.2 del enunciado). Cada captura muestra:

1. el request ejecutado con **Try it out**;
2. la respuesta del servidor: status, body de error con `correlationId` y el header `X-Correlation-Id`;
3. debajo, los ejemplos documentados de todos los códigos de error de ese endpoint.

| Captura | Request | Respuesta |
|---|---|---|
| [01-products-404-PRD-001.png](01-products-404-PRD-001.png) | `GET /api/products/99` (el ejemplo del enunciado) | 404 · PRD-001 · Producto no encontrado. |
| [02-products-409-PRD-004.png](02-products-409-PRD-004.png) | `DELETE` de la Notebook, con una orden Pendiente en Orders.API | 409 · PRD-004 · El producto tiene órdenes activas y no puede eliminarse. |
| [03-users-403-USR-004.png](03-users-403-USR-004.png) | `POST /api/users/login` de `juan@email.com`, bloqueado por intentos | 403 · USR-004 · Su cuenta fue bloqueada por superar el máximo de intentos fallidos. |
| [04-users-400-USR-002.png](04-users-400-USR-002.png) | `POST /api/users/register` sin nombre y con un email inválido | 400 · USR-002 · Los dos problemas separados por punto y coma |
| [05-orders-422-ORD-005.png](05-orders-422-ORD-005.png) | `POST /api/orders` con 5 taladros (stock 2) | 422 · ORD-005 · Stock insuficiente para 'Taladro percutor 750W'. |
| [06-cart-422-CRT-003.png](06-cart-422-CRT-003.png) | `POST /api/cart/{userId}/items` con 5 taladros | 422 · CRT-003 · Stock insuficiente. Disponible: 2, solicitado: 5. |
| [07-notifications-404-NTF-001.png](07-notifications-404-NTF-001.png) | `POST /api/notifications/send` a un usuario que no existe en Users.API | 404 · NTF-001 · El usuario destinatario no fue encontrado. |

Las capturas 02, 05, 06 y 07 dependen de otro servicio (Orders, Products y Users): muestran la comunicación entre servicios funcionando.

## Cómo regenerarlas

Con los cinco servicios recién levantados (`scripts/start-all.sh`, así los datos semilla están intactos):

```bash
python3 -m venv .venv
```

```bash
.venv/bin/pip install playwright && .venv/bin/python -m playwright install chromium
```

```bash
.venv/bin/python scripts/capture-swagger.py
```

El script ([`scripts/capture-swagger.py`](../../scripts/capture-swagger.py)) abre Swagger UI con Playwright, ejecuta cada request y guarda las capturas en esta carpeta.

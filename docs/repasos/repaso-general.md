# Repaso general del proyecto

Explicación de todo lo hecho en el repositorio hasta el cierre de Orders.API (10/10/2026): de dónde partimos, cómo planificamos, cómo nos organizamos, qué arquitectura elegimos y por qué, qué aprendimos de las revisiones de código y en qué estado está cada servicio.

Este documento da la **vista de conjunto**. El detalle técnico de cómo se construyó la plantilla (controllers, manejo de errores, logs, Swagger y health checks) está en [repaso-products-api.md](repaso-products-api.md); lo propio de cada API, en su repaso: [Products](repaso-products-api.md), [Cart](repaso-cart-api.md), [Users](repaso-users-api.md), [Notifications](repaso-notifications-api.md) y [Orders](repaso-orders-api.md).

| Documento | Para qué sirve |
|---|---|
| [TP_Microservicios_ECommerce_v7.md](../consignas/TP_Microservicios_ECommerce_v7.md) | Las consignas de la cátedra (versión Markdown del `.docx`) |
| [plan-de-desarrollo.md](../planificacion/plan-de-desarrollo.md) | La guía de trabajo: decisiones, reparto, convenciones y etapas con sus tareas |
| [arquitectura.md](../arquitectura/arquitectura.md) | Diagramas del sistema y de clases |
| [repaso-products-api.md](repaso-products-api.md) | Cómo se construyó Products.API, decisión por decisión |
| [repaso-cart-api.md](repaso-cart-api.md) | Cómo se construyó Cart.API, el primer servicio que consume a otro |
| [repaso-users-api.md](repaso-users-api.md) | Cómo se construyó Users.API: contraseñas, bloqueo y el contrato con Orders y Notifications |
| [repaso-notifications-api.md](repaso-notifications-api.md) | Cómo se construyó Notifications.API: envío simulado y cliente de Users |
| [repaso-orders-api.md](repaso-orders-api.md) | Cómo se construyó Orders.API: máquina de estados y clientes de Users y Products |
| **Este documento** | La vista de conjunto del proyecto |

---

## Índice

1. [Línea de tiempo](#1-línea-de-tiempo)
2. [El punto de partida](#2-el-punto-de-partida)
3. [La planificación](#3-la-planificación)
4. [Cómo trabajamos en equipo](#4-cómo-trabajamos-en-equipo)
5. [La arquitectura del sistema](#5-la-arquitectura-del-sistema)
6. [La estructura del repositorio](#6-la-estructura-del-repositorio)
7. [La plantilla común de cada API](#7-la-plantilla-común-de-cada-api)
8. [Cart.API: el primer servicio que consume a otro](#8-cartapi-el-primer-servicio-que-consume-a-otro)
9. [Users.API, Notifications.API y Orders.API](#9-usersapi-notificationsapi-y-ordersapi)
10. [La documentación del repositorio](#10-la-documentación-del-repositorio)
11. [Revisiones de código: qué aprendimos](#11-revisiones-de-código-qué-aprendimos)
12. [Estado actual de cada servicio](#12-estado-actual-de-cada-servicio)
13. [Las decisiones, agrupadas por tema](#13-las-decisiones-agrupadas-por-tema)
14. [Próximos pasos](#14-próximos-pasos)
15. [Preguntas generales de la defensa](#15-preguntas-generales-de-la-defensa)

---

## 1. Línea de tiempo

| Fecha | Quién | Qué pasó | Commit |
|---|---|---|---|
| 16/09 | Thomas | Creación del repositorio | `61b6761` |
| 18/09 | Thomas | Se sube la plantilla `MiniApi` de la cátedra a `develop` | `39d99cf` |
| 27/09 | Thomas | Plan de desarrollo con reparto de tareas | `1e19b88` |
| 27/09 | Thomas | **Etapa 0:** solución con las 5 APIs y sus 5 proyectos de tests | `7863d65` |
| 27/09 | Thomas | GitHub Actions: compila y corre los tests en cada push | `6d8c7b1` |
| 27/09 | Thomas | Contratos entre servicios acordados con Juan Pablo | `cfac238` |
| 27/09 | — | **Hito H1:** PR #1 de `develop` a `main` | merge `4618329` |
| 27/09 | Thomas | **Etapa 1:** dominio y lógica de negocio de Products | `8220c39` |
| 27–29/09 | Juan Pablo | Users.API: lógica, repositorio, refactor a `async` y tests | `1dfb312` … `a31c5d6` |
| 29/09 | Juan Pablo | Notifications.API: dominio, servicio, tests y controller | `f7451f0`, `f62dd6f` |
| 30/09 | Thomas | Convenciones de código y diagramas de arquitectura | `d4f5b45` |
| 30/09 | Thomas | Consignas en `.docx` y Markdown | `7891229` |
| 30/09 | Thomas | **Etapa 2:** endpoints y contrato de errores | `01236c2` |
| 30/09 | Thomas | **Etapa 3:** Serilog y Correlation ID | `6906025` |
| 30/09 | Thomas | **Etapa 4:** Swagger y health checks. Products.API completo | `f2b4118` |
| 30/09 | Thomas | Repasos general y de Products.API | `482fd1c` |
| 01/10 | Thomas | **Etapa 6 (parte 1):** lógica del carrito y `ProductsClient`, el primer cliente HTTP entre servicios | `0405e34` |
| 01/10 | Thomas | **Etapa 6 (parte 2):** endpoints de Cart y plantilla replicada. Cart.API completo | `e4a56ea` |
| 04/10 | Juan Pablo | Users: `GET /api/users/{id}`, contrato de errores y D-09. Notifications: repositorio Singleton y envío simulado. Cierra los bloqueantes de la segunda revisión | `6b16f06`, `a3ad10e`, `5d673a4` |
| 07/10 | Juan Pablo | Correcciones de la tercera revisión: `UsersClient` real, validaciones en español, repositorios thread-safe, datos semilla y tests de integración | `f927227`, `54a8efa` |
| 07/10 | Juan Pablo | Alineación con el plan: métodos en inglés, enums de Notifications y acciones del controller como en la plantilla | `aba9c44`, `76b4cee` |
| 07/10 | Juan Pablo | **Etapas 5 y 8:** plantilla transversal replicada. Users.API y Notifications.API completos | `348fb38` |
| 10/10 | Juan Pablo | **Etapa 7:** Orders.API en cuatro tandas: dominio y máquina de estados, servicio, clientes HTTP y capa HTTP con la plantilla. Orders.API completo | `f34b388` … `52d05e1` |

En poco más de tres semanas se pasó de una plantilla vacía a los cinco servicios completos (426 tests entre los cinco), tres de ellos consumiendo a otros. Falta la integración de punta a punta (Etapa 9).

---

## 2. El punto de partida

### 2.1 Lo que recibimos

- **Las consignas:** cinco microservicios (Products, Users, Orders, Cart y Notifications) en .NET 10, con requisitos funcionales (endpoints y catálogos de errores) y transversales (Swagger, manejo de errores, logs, Correlation ID y health checks).
- **La plantilla `MiniApi`:** un proyecto mínimo de ASP.NET con el ejemplo del clima y un `.gitignore`.
- **Una promesa:** la persistencia la va a proveer la cátedra como librería. **Todavía no llegó.**

### 2.2 Lo primero: leer las consignas buscando huecos

Antes de escribir código, leímos el enunciado buscando qué **no** estaba definido. Aparecieron varios huecos, y cada uno se resolvió con una decisión propia documentada:

| Hueco en el enunciado | Problema | Decisión |
|---|---|---|
| Orders y Notifications tienen que verificar que el usuario exista (ORD-003, NTF-001)… | …pero Users.API solo expone `register` y `login` | **D-06:** agregar `GET /api/users/{id}`, con el código nuevo USR-007 |
| Products no puede borrar un producto con órdenes activas (PRD-004)… | …pero Orders solo filtra por `?usuarioId=` | **D-07:** agregar el filtro `?productoId=` |
| USR-004 (bloqueo por intentos) y USR-005 (bloqueo manual) son distintos… | …pero el modelo `User` no guarda el motivo del bloqueo | **D-08:** se deduce de `IntentosFallidos` |
| ¿El tercer intento fallido responde 401 o 403? | No está definido | **D-09:** 401 y bloquea; los siguientes, 403 |
| `BusinessRuleException` del Apéndice B no tiene status… | …pero el catálogo usa reglas de negocio con 401, 403, 409 y 422 | **D-10:** la excepción lleva el status |
| La persistencia es de la cátedra… | …y todavía no existe | **D-03:** repositorios en memoria detrás de interfaces |

**Por qué importa:** si estos huecos se descubrían en medio del desarrollo, cada uno habría significado rehacer trabajo de los dos. Resolverlos al principio permitió fijar los **contratos entre servicios** antes de dividir el trabajo (sección 4.3).

---

## 3. La planificación

### 3.1 Un plan escrito y versionado

Todo el trabajo sigue [plan-de-desarrollo.md](../planificacion/plan-de-desarrollo.md), que está en el repo y se actualiza a medida que avanzamos (las casillas se marcan al terminar cada tarea). Tiene seis partes:

1. **Resumen del sistema:** servicios, responsables y puertos.
2. **Estructura:** carpetas, y qué clases llevan interfaz y cuáles no.
3. **Forma de trabajo:** git, hitos, TDD, principios de diseño y convenciones de código.
4. **Reparto de tareas:** quién hace qué y en qué orden.
5. **Registro de decisiones:** D-01 a D-24, cada una con su motivo.
6. **Etapas:** la 0 a la 11, con tareas y criterio de "lista cuando".

**¿Por qué tanto detalle?** Por tres razones:

- Somos dos personas trabajando en paralelo sobre la misma rama; sin un plan compartido, cada uno resolvería lo mismo de forma distinta.
- El enunciado exige que cada integrante pueda explicar cualquier parte del código.
- Las decisiones escritas evitan volver a discutir lo mismo.

### 3.2 Etapas pequeñas e iterativas

En lugar de construir todo de una vez, el trabajo se dividió en 12 etapas. Cada una termina con algo que compila, pasa sus tests y se puede subir:

| Etapas | Qué cubren | Responsable |
|---|---|---|
| 0 | Esqueleto de la solución | Thomas |
| 1 – 4 | Products.API completo: es la **plantilla** de las demás | Thomas |
| 5 | Users.API | Juan Pablo |
| 6 | Cart.API | Thomas |
| 7 | Orders.API | Juan Pablo |
| 8 | Notifications.API | Juan Pablo |
| 9 | Integración entre servicios | Ambos |
| 10 – 11 | Documentación, prueba integral y defensa | Ambos |

**¿Por qué Products primero y completo?** Así lo sugiere la sección 10 del enunciado: "Products.API es la plantilla: una vez que funciona, la estructura se replica en el resto". Resolver una sola vez los aspectos transversales (errores, logs, Swagger y health checks) y después copiarlos evita resolverlos cinco veces de cinco formas distintas.

### 3.3 Hitos

Al cerrar cada hito se abre un PR de `develop` a `main`:

| Hito | Contenido | Estado |
|---|---|---|
| H1 | Solución compilando | ✅ PR #1 mergeado (27/09) |
| H2 | Products completo y Users con su contrato de errores | 🟡 Products listo; falta la parte de Users |
| H3 | Los cinco servicios funcionando | 🟡 Products y Cart listos |
| H4 | Integración, documentación y entrega | Pendiente |

### 3.4 TDD como método

Todo el código de Products se escribió con TDD: primero el test, verlo fallar y después el código. Además de dar confianza para cambiar el código, **encontró tres bugs que una prueba manual difícilmente habría encontrado** (detalle en el [repaso de Products](repaso-products-api.md#13-cómo-trabajamos-tdd)). La meta es que los otros servicios sigan el mismo método.

---

## 4. Cómo trabajamos en equipo

### 4.1 Reparto por servicios

| | Thomas | Juan Pablo |
|---|---|---|
| Servicios | Products, Cart | Users, Notifications, Orders |
| Transversales | La plantilla (errores, logs, Swagger y health checks) | Replicarla en sus servicios |

**La lógica del reparto:** cada uno es dueño de los servicios que consumen a otro servicio suyo. Cart consume a Products (Thomas); Orders y Notifications consumen a Users (Juan Pablo). Así cada uno conoce bien los contratos que usa. Thomas tiene la parte más pesada (la plantilla) y Juan Pablo más servicios, que arma replicándola.

### 4.2 Trabajo en paralelo en bloques

El trabajo se ordenó en 7 bloques. En cada bloque los dos avanzan en paralelo y al final se sincronizan (sección 4.2 del plan). El truco que lo hace posible es separar cada servicio en partes que **no dependen de la plantilla**. Por ejemplo, Juan Pablo puede escribir la lógica de negocio de Users con sus tests unitarios mientras Thomas termina la capa HTTP de Products.

### 4.3 Contratos acordados antes de empezar

Para que nadie tenga que esperar al otro, en el Bloque 1 se fijaron (sección 4.4 del plan):

- **La forma de las excepciones:** `NotFoundException(errorCode, message)`, `BusinessRuleException(errorCode, message, statusCode)` y `ValidationException(errorCode, message)`.
- **El formato de error:** el de la sección 3.1 del enunciado, más `correlationId`.
- **Los endpoints que se consumen entre servicios:** `GET /api/products/{id}`, `GET /api/users/{id}` y `GET /api/orders?productoId=`.
- **Los nombres de los clientes HTTP:** `IProductsClient`, `IUsersClient` e `IOrdersClient`.

Con esto, Thomas puede programar el cliente de Orders y Juan Pablo los clientes de Users **antes** de que existan los servicios del otro: testean contra el contrato acordado con dobles de prueba.

### 4.4 Git y commits

- **Una sola rama de trabajo: `develop`.** No usamos feature branches; es un TP y alcanza con una rama compartida y PRs a `main` en cada hito.
- **Commits con Commitizen** (`cz commit`), con el formato Conventional Commits: `tipo(alcance): descripción`. Por ejemplo: `feat(products): agregar endpoints…`. El historial se lee como una lista de cambios.
- **Cada uno toca solo sus carpetas,** y hace `git pull --rebase` antes de cada push. Por eso no hubo conflictos.
- **Merge commit, no squash, al pasar a `main`.** Con squash o rebase, GitHub crea en `main` commits que `develop` no tiene, y el siguiente PR mostraría conflictos o commits repetidos.

### 4.5 Integración continua

GitHub Actions ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)) compila la solución y corre **todos** los tests en cada push a `develop` o `main` y en cada PR a `main`. La regla "nunca se mergea código que no compile" no depende de que alguien se acuerde de verificarlo.

**Una limitación importante:** el CI solo ve lo que se compila. Un archivo sin extensión `.cs` no entra en la compilación, así que el CI sigue en verde aunque el código tenga errores. Esto pasó de verdad (sección 11.1).

---

## 5. La arquitectura del sistema

### 5.1 Microservicios

```mermaid
flowchart LR
    cliente(["Cliente"])
    products["Products.API :5001"]
    users["Users.API :5002"]
    orders["Orders.API :5003"]
    cart["Cart.API :5004"]
    notifications["Notifications.API :5005"]

    cliente --> products & users & orders & cart & notifications
    cart -- "GET /api/products/{id}" --> products
    orders -- "GET /api/products/{id}" --> products
    orders -- "GET /api/users/{id}" --> users
    notifications -- "GET /api/users/{id}" --> users
    products -. "GET /api/orders?productoId=" .-> orders
```

Cada servicio:

- **Es un proyecto independiente** con su propio puerto. Se puede levantar, testear y desplegar solo.
- **Es dueño de sus datos.** Ningún servicio lee los datos de otro directamente: si Orders necesita saber si un usuario existe, se lo pregunta a Users por HTTP.
- **Tiene su propia copia de la plantilla** (D-05: sin librería compartida).

### 5.2 ¿Por qué no hay una librería compartida (D-05)?

Los cinco servicios tienen las mismas excepciones, handlers y middlewares. Parecería lógico moverlos a una librería común. No lo hicimos por dos razones:

1. **Autonomía:** en una arquitectura de microservicios, cada servicio debería poder cambiar sin coordinar con los demás. Una librería compartida obliga a actualizar los cinco cada vez que cambia.
2. **El enunciado:** la sección 6 muestra `Exceptions/` y `ExceptionHandlers/` dentro de cada API.

El costo es duplicar código. Lo aceptamos a cambio de que cada servicio sea independiente.

### 5.3 La dependencia circular Products ↔ Orders

Orders llama a Products (precio y stock) y Products llama a Orders (PRD-004). Es la única dependencia en los dos sentidos. Es aceptable porque:

- son llamadas puntuales, no un flujo continuo entre los dos;
- Products la tiene detrás de `IOrdersClient`, y desde la Etapa 9 la implementa `OrdersClient` con HTTP real;
- si Orders se cae, solo falla la eliminación de productos (500 con PRD-005, sin borrar, D-39), no todo Products: su `/health/ready` queda `Degraded`.

Conviene tenerla clara para la defensa, porque es una pregunta típica.

### 5.4 Comunicación entre servicios

Las llamadas HTTP se hacen con `IHttpClientFactory`, siempre detrás de una interfaz (`IProductsClient`, `IUsersClient`, `IOrdersClient`), con la URL de cada servicio en `appsettings.json`. Cart → Products ya funciona así (sección 8).

En la Etapa 9 cada llamada empieza a llevar el `X-Correlation-Id` del request original, con un `CorrelationIdDelegatingHandler`. Products → Orders ya lo hace: un `DELETE` de un producto aparece con el mismo ID en los logs de los dos servicios ([repaso de Products, sección 7.3](repaso-products-api.md#73-correlationiddelegatinghandler-el-id-cruza-servicios)). Faltan Cart, Orders y Notifications.

---

## 6. La estructura del repositorio

```
tp_arquitectura_microservicios_cai_grupo_15/
├── .github/workflows/ci.yml      # integración continua
├── ECommerce.slnx                 # la solución con los 10 proyectos
├── src/                           # las 5 APIs (puertos 5001–5005)
├── tests/                         # 5 proyectos de tests, cada uno con Unit/ e Integration/
├── docs/                          # consignas, plan, arquitectura y repasos
├── .gitignore
└── README.md                      # se completa en la Etapa 10
```

Decisiones de estructura (detalle en el [repaso de Products](repaso-products-api.md#2-etapa-0--esqueleto-de-la-solución)):

| Decisión | Por qué |
|---|---|
| `ECommerce.slnx` reemplaza a `MiniApi.slnx` | Estructura de la sección 6 del enunciado (D-01) |
| Controllers, no Minimal API | El enunciado pide `Controllers/` y XML comments (D-02) |
| Solo HTTP, puertos fijos 5001–5005 | Llamadas internas simples, sin certificados (D-16) |
| `appsettings.Development.json` versionado | Define el detalle de errores por entorno (D-04) |
| Un proyecto de tests por API | Los tests no forman parte del producto y cada uno trabaja en sus carpetas |
| Paquetes de tests | xUnit, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), NSubstitute (dobles de prueba) y `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) |

---

## 7. La plantilla común de cada API

Todas las APIs tienen las mismas capas. El detalle de cada una, con su porqué, está en el [repaso de Products](repaso-products-api.md):

| Capa / pieza | Responsabilidad | Requisito del enunciado |
|---|---|---|
| Controller | Traduce HTTP ↔ DTO, sin lógica ni `try/catch` | Endpoints (sección 4) |
| Service | Reglas de negocio; lanza excepciones con código del catálogo | Catálogos de errores |
| Repository | Persistencia (en memoria hasta tener la librería) | Persistencia de la cátedra |
| Clients | Llamadas a otros servicios | Comunicación HTTP |
| Excepciones + 4 `IExceptionHandler` + `ErrorResponseWriter` | Excepción → JSON de error del contrato | 5.2 Manejo de errores |
| Serilog + `CorrelationIdMiddleware` + `RequestLoggingMiddleware` | Logs estructurados con Servicio, Endpoint, CorrelationId y ErrorCode | 5.3 Logging y 5.5 Correlation ID |
| Swashbuckle + `[ProducesError]` | Documentación con ejemplos de éxito y de error | 5.1 Swagger |
| `/health`, `/health/ready`, `/health/live` | Estado del servicio | 5.4 Health Checks |

**La plantilla ya se replicó una vez, en Cart.API**, y sirvió: la parte transversal (manejo de errores, logs, Correlation ID, Swagger y health checks) se copió de Products y solo hubo que adaptar los códigos de error y dos detalles ([repaso de Cart, sección 5.2](repaso-cart-api.md#52-replicar-la-plantilla)).

**El criterio para usar interfaces** (D-15, sección 2.3 del plan): llevan interfaz los servicios, la persistencia, los clientes de otros servicios y lo que depende del entorno. No llevan interfaz los modelos, los DTOs, las excepciones, los controllers ni las reglas puras. Una interfaz tiene que aportar algo: poder reemplazar la pieza en un test o cambiar su implementación.

---

## 8. Cart.API: el primer servicio que consume a otro

Cart.API es el segundo servicio terminado y el primero que **depende de otro**: para agregar un producto al carrito, le pregunta a Products.API por HTTP si existe y cuánto stock tiene. El detalle completo está en su repaso: **[repaso-cart-api.md](repaso-cart-api.md)**. Lo importante, a nivel proyecto:

| Tema | Qué se hizo | Más detalle |
|---|---|---|
| **Huecos del enunciado** | Cuatro decisiones nuevas antes de escribir tests: todo 400 usa CRT-004 (D-25), un producto que no está en el carrito responde CRT-002 (D-26), vaciar elimina el carrito (D-27) y Products caído responde CRT-005 (D-28) | [Repaso de Cart, sección 2](repaso-cart-api.md#2-antes-de-programar-los-huecos-del-enunciado) |
| **Primer cliente HTTP** | `ProductsClient`, registrado con `IHttpClientFactory` y con la URL en `appsettings.json`. Revisa el status antes de leer el body: un 404 es un dato ("no existe") y un 500 es una falla | [Repaso de Cart, sección 4](repaso-cart-api.md#4-productsclient-hablar-con-otro-servicio) |
| **Tests con una dependencia** | Tres niveles: el cliente con un handler HTTP falso, Cart completo con un `FakeProductsClient` y una prueba manual con los dos servicios | [Repaso de Cart, sección 6](repaso-cart-api.md#6-cómo-se-testea-un-servicio-que-depende-de-otro) |
| **La plantilla se replicó** | La parte transversal se copió de Products y solo cambiaron los códigos de error y dos detalles. Confirma que la plantilla no depende del dominio | [Repaso de Cart, sección 5.2](repaso-cart-api.md#52-replicar-la-plantilla) |
| **Resiliencia** | Con Products caído, Cart responde CRT-005 al agregar productos, pero sigue mostrando los carritos | [Repaso de Cart, sección 7](repaso-cart-api.md#7-la-prueba-con-los-dos-servicios-levantados) |
| **Lo que falta** | El Correlation ID no viaja de Cart a Products: un mismo request aparece con dos IDs distintos en los logs. Lo resuelve la Etapa 9 | [Repaso de Cart, sección 10](repaso-cart-api.md#10-lo-que-falta-etapa-9) |

> **Para la defensa:** "Cart consulta a Products por HTTP detrás de una interfaz. Un 404 de Products es un dato del negocio (CRT-002) y un 500 es una falla de infraestructura (CRT-005). Si Products se cae, solo fallan las operaciones que lo necesitan."

---

## 9. Users.API, Notifications.API y Orders.API

Los tres servicios de Juan Pablo. El detalle está en sus repasos: **[repaso-users-api.md](repaso-users-api.md)**, **[repaso-notifications-api.md](repaso-notifications-api.md)** y **[repaso-orders-api.md](repaso-orders-api.md)**. Lo importante, a nivel proyecto:

| Tema | Qué se hizo | Más detalle |
|---|---|---|
| **Users es un proveedor** | No consume a nadie, pero Orders y Notifications dependen de `GET /api/users/{id}` (D-06). Un cambio de contrato no rompe los tests de Users: rompe a los otros | [Repaso de Users, sección 5](repaso-users-api.md#5-el-contrato-con-orders-y-notifications) |
| **Contraseñas y bloqueo** | Hash con `PasswordHasher<User>` del framework. El bloqueo vive en `AccountLockoutPolicy`: USR-004 vs. USR-005 por los intentos (D-08) y el tercer intento responde 401 (D-09) | [Repaso de Users, sección 3](repaso-users-api.md#3-el-dominio-y-las-reglas) |
| **Datos de la demo** | Tres usuarios semilla: María (activa), Juan (bloqueado por intentos) y Carlos (bloqueado a mano, para mostrar USR-005) | [Repaso de Users, sección 3.7](repaso-users-api.md#37-datos-semilla) |
| **Segundo cliente HTTP** | `UsersClient` en Notifications, con el mismo diseño que `ProductsClient` de Cart y un timeout de 5 segundos | [Repaso de Notifications, sección 4](repaso-notifications-api.md#4-usersclient-hablar-con-usersapi) |
| **Envío simulado** | Detrás de `INotificationSender`: un envío real sería una clase nueva, sin tocar el servicio | [Repaso de Notifications, sección 3.2](repaso-notifications-api.md#32-el-envío-detrás-de-una-interfaz) |
| **Huecos nuevos** | D-29 (Users caído → NTF-004), D-30 (el GET no consulta a Users), D-31 (se notifica a usuarios bloqueados) y D-32 (validación del email) | [Repaso de Notifications, sección 2](repaso-notifications-api.md#2-antes-de-programar-los-huecos-del-enunciado) |
| **La plantilla se replicó otra vez** | Copiada de Cart cambiando solo el namespace, los códigos de error y el health check | Repasos, sección 4.2 / 5.2 |
| **Orders: la máquina de estados** | Una tabla en `OrderStatusTransitions` con las transiciones permitidas; cualquier otra es ORD-006. Testeada con las 25 combinaciones | [Repaso de Orders, sección 3.2](repaso-orders-api.md#32-la-máquina-de-estados-orderstatustransitions) |
| **Orders consume a dos servicios** | Users (ORD-003) y Products (precio, ORD-004 y ORD-005), con clientes copiados de Notifications y Cart y timeout de 5 segundos. Todos los chequeos se hacen antes de guardar | [Repaso de Orders, sección 3.3](repaso-orders-api.md#33-las-reglas-de-orderservice) |
| **El contrato con Products** | `GET /api/orders?productoId=` (D-07), que Thomas va a usar para PRD-004 en la Etapa 9 | [Repaso de Orders, sección 10](repaso-orders-api.md#10-lo-que-falta-etapa-9) |
| **Huecos de Orders** | D-33 (`FechaActualizacion`), D-34 (items repetidos), D-35 (usuarios bloqueados), D-36 (servicio caído → ORD-007), D-37 (filtro inválido → `[]`) y D-38 (el 409 del POST) | [Repaso de Orders, sección 2](repaso-orders-api.md#2-antes-de-programar-los-huecos-del-enunciado) |

> **Para la defensa:** "Users es el servicio del que dependen otros, por eso su contrato está fijado en un test. Notifications le pregunta a Users si el destinatario existe; un 404 es NTF-001 y una falla de Users es NTF-004. Orders le pregunta a Users y a Products antes de guardar nada, toma el precio de Products y controla los cambios de estado con una tabla de transiciones."

---

## 10. La documentación del repositorio

| Documento | Cómo se armó | Por qué |
|---|---|---|
| **Consignas** (`.docx` + `.md`) | El `.md` se generó con un conversor propio en Python que lee el XML interno del `.docx`. Toma la fuente Courier New como señal de código para separar los bloques JSON y C#, y convierte las 47 tablas según su tipo. Se verificó palabra por palabra contra el original. | GitHub no muestra los `.docx`. En Markdown se lee, se busca y se puede enlazar a una sección. Si la cátedra publica una v8, `git diff` muestra qué cambió. |
| **Plan de desarrollo** | Se fue completando etapa por etapa. | Es la guía del equipo; las decisiones quedan escritas. |
| **Arquitectura** | Diagramas en Mermaid: GitHub los dibuja solos y se editan como texto. | Una imagen se desactualiza; el texto se versiona junto al código. |
| **Repasos** (`docs/repasos/`) | Uno general y uno por API, escrito al terminarla (convención del plan). | Para entender el código en profundidad y preparar la defensa. |
| **README** | La puerta de entrada del repo: qué es, cómo ejecutarlo, puertos, estado y enlaces a los demás documentos. La tabla de códigos de error y las decisiones se completan en la Etapa 10. | Es lo primero que ve cualquiera que abre el repositorio, incluidos los docentes. |

Todos los diagramas Mermaid se verificaron dibujándolos en un navegador antes de subirlos. Así se encontraron dos problemas que en GitHub se habrían visto como errores: diagramas de clases enredados y un `;` que rompía un diagrama de secuencia.

---

## 11. Revisiones de código: qué aprendimos

Revisamos el código de Juan Pablo tres veces. No fue para señalar errores, sino porque el enunciado exige que los dos entiendan todo el código, y porque los problemas que se encuentran temprano se corrigen fácil.

### 11.1 Primera revisión: archivos que no compilaban

**Lo que se encontró:** casi todos los archivos de Users estaban **sin extensión `.cs`** y muchos estaban vacíos. Como .NET solo compila los `.cs`, el compilador los ignoraba, y el build y el CI daban verde aunque el código tuviera errores.

**Causa probable:** en Windows, el Explorador oculta las extensiones; un archivo creado como "UserService" queda sin `.cs`.

**Lo que aprendimos:** "el CI está verde" no significa "el código funciona", si el código no entra en la compilación. Por eso se sumó la convención "revisar `git status` antes de cada commit" y crear las clases desde el editor.

### 11.2 Segunda revisión: mejoró mucho, con tres bloqueantes

Juan Pablo corrigió la primera revisión: archivos con extensión, métodos `async` con `CancellationToken`, DTOs como `record`, validaciones y tests unitarios. Aparecieron tres problemas nuevos que **los tests unitarios no podían detectar**:

| Bloqueante | Por qué los tests no lo vieron |
|---|---|
| Se eliminó `GET /api/users/{id}` y USR-007, que son un contrato acordado (D-06) | Users compila y pasa sus tests; el que se rompe es **otro** servicio (Orders y Notifications), y recién cuando se integran |
| Users.API no registra sus dependencias: la API real responde 500 en `register` y `login` | Los tests unitarios crean el servicio a mano (`new UserService(...)`) y nunca pasan por el contenedor |
| El repositorio de Notifications es Scoped: las notificaciones se pierden entre requests | Mismo motivo: el test no usa el contenedor de dependencias |

También hubo observaciones importantes, no bloqueantes: el tercer intento fallido de login no coincide con D-09, la `BusinessRuleException` de Notifications no tiene `statusCode`, los repositorios usan `List<T>` (no es thread-safe), y faltan tests, datos semilla y `TimeProvider`.

### 11.3 Lo que se agregó al plan a partir de las revisiones

Las dos revisiones dieron origen a la sección **"Convenciones de código"** del plan: 12 reglas concretas, cada una con su porqué. Las más importantes:

- archivos siempre con `.cs`, creados desde el editor;
- repositorios en memoria **Singleton** y con `ConcurrentDictionary`;
- un **`DependencyInjectionTests` en cada API**, que levanta la app real y verifica que el contenedor resuelva el servicio. Es el test que habría detectado los bloqueantes 2 y 3;
- los contratos entre servicios no se cambian sin acordarlo.

**La lección general:** cada tipo de test detecta un tipo de error distinto. Los unitarios prueban la lógica; los de integración, que las piezas estén bien conectadas; los contratos entre servicios se rompen en el otro servicio. Por eso Products tiene los tres tipos de verificación.

### 11.4 Tercera revisión: siete puntos y la alineación con el plan

Los bloqueantes de la segunda revisión se resolvieron el 04/10. La tercera encontró siete puntos más:

| Servicio | Lo que se encontró | Por qué importaba |
|---|---|---|
| Notifications | Seguía registrado `StubUsersClient`; `UsersClient.cs` estaba vacío | NTF-001 nunca se validaba de verdad: cualquier usuario "existía" |
| Notifications | `Guid UsuarioId` con `[Required]` | Un body sin `usuarioId` respondía 404 (NTF-001) en lugar de 400 (NTF-002) |
| Notifications | Ruta `{userId:guid}` | `/api/notifications/99` respondía un 404 vacío, sin `errorCode` (D-17) |
| Notifications | Clave `Services:Users:BaseUrl` | No era la acordada (`Services:UsersApi:BaseUrl`) |
| Users | Validaciones sin mensaje en español | El `errorMessage` de USR-002 salía en inglés, y un email vacío daba dos errores |
| Users | `List<T>` y sin datos semilla | No era thread-safe, y USR-005 no se podía mostrar en la demo |
| Users | Pocos tests | Faltaban `GetById`, USR-005, el hash y los de integración |

Todo se corrigió el 07/10 (`f927227`, `54a8efa`). Después se revisaron los dos servicios contra **todas** las decisiones y convenciones del plan, y aparecieron diferencias de prolijidad: métodos en español, tipo y estado de Notifications como texto en lugar de enums, y acciones del controller con otro estilo. Se corrigieron (`aba9c44`, `76b4cee`) y se replicó la plantilla transversal (`348fb38`). Users pasó de 6 a 72 tests y Notifications de 3 a 57.

**La lección:** "compila y pasa los tests" no alcanza si los tests no prueban el contrato. Un stub que dice "sí" a todo hace pasar cualquier test.

---

## 12. Estado actual de cada servicio

| Servicio | Responsable | Estado | Tests |
|---|---|---|---|
| **Products.API** | Thomas | ✅ **Completo** (Etapas 1–4). Es la plantilla. Integrado con Orders (Etapa 9): `OrdersClient` real, Correlation ID saliente y Orders en `/health/ready`. | 97 |
| **Users.API** | Juan Pablo | ✅ **Completo** (Etapa 5). Es el servicio del que dependen Orders y Notifications. Sin tareas en la Etapa 9. | 72 |
| **Notifications.API** | Juan Pablo | ✅ **Completo** (Etapa 8). Consume a Users. Falta propagar el Correlation ID y sumar Users a `/health/ready` (Etapa 9). | 57 |
| **Cart.API** | Thomas | ✅ **Completo** (Etapa 6). Primer servicio que consume a otro. Falta propagar el Correlation ID y sumar Products a `/health/ready` (Etapa 9). | 90 |
| **Orders.API** | Juan Pablo | ✅ **Completo** (Etapa 7). Consume a Users y Products. Falta propagar el Correlation ID y sumar Users y Products a `/health/ready` (Etapa 9). | 136 |

**Pendientes externos:**

- La librería de persistencia de la cátedra (D-03).
- Consultar con los docentes los endpoints adicionales (D-06, D-07), el código USR-007 y el 409 de `POST /api/orders` (D-38).

---

## 13. Las decisiones, agrupadas por tema

Las 38 decisiones del plan, agrupadas para entender **qué problema resuelve cada grupo**. El texto completo está en la sección 5 del plan.

**Estructura y entorno**

| # | Decisión | En una línea |
|---|---|---|
| D-01 | `ECommerce.slnx` con `src/` y `tests/` | Estructura de la sección 6 del enunciado más TDD |
| D-02 | Controllers | El enunciado pide la carpeta `Controllers/` |
| D-04 | `appsettings.Development.json` versionado | Define el detalle de errores por entorno |
| D-16 | Solo HTTP | Llamadas internas sin certificados |

**Diseño**

| # | Decisión | En una línea |
|---|---|---|
| D-03 | Persistencia en memoria detrás de interfaces | Avanzar sin esperar la librería de la cátedra |
| D-05 | Sin librería compartida | Autonomía de cada microservicio |
| D-15 | Interfaces solo donde aportan | Bajo acoplamiento sin interfaces innecesarias |

**Huecos del enunciado (contratos entre servicios y reglas)**

| # | Decisión | En una línea |
|---|---|---|
| D-06 | `GET /api/users/{id}` + USR-007 | Orders y Notifications verifican usuarios |
| D-07 | `?productoId=` en Orders | Products verifica órdenes activas |
| D-08 | USR-004 vs. USR-005 por `IntentosFallidos` | El modelo no guarda el motivo del bloqueo |
| D-09 | Tercer intento: 401 y bloqueo | La respuesta refleja el motivo de ese intento |
| D-13 | Cart suma cantidades | Comportamiento esperable de un carrito |
| D-14 | Crear una orden no descuenta stock | El enunciado no lo exige |

**Contrato de errores**

| # | Decisión | En una línea |
|---|---|---|
| D-10 | `BusinessRuleException` con status | El catálogo usa 401, 403, 409 y 422 |
| D-11 | `correlationId` en los errores | Requisito 5.5 |
| D-12 | Solo los status del contrato | `PUT` de productos no devuelve 409 |
| D-17 | Ids de ruta como texto | `/api/products/99` → 404 con PRD-001 |
| D-18 | Detalle de los 500 según entorno | Requisito 5.2, sin stack trace |
| D-19 | JSON inválido → un mensaje en español | Los mensajes de .NET están en inglés |

**Cart.API**

| # | Decisión | En una línea |
|---|---|---|
| D-25 | Todo 400 de Cart usa CRT-004 | Es el único código 400 del catálogo de Cart |
| D-26 | Producto que no está en el carrito → CRT-002 | El catálogo no tiene un código para ese caso |
| D-27 | Vaciar elimina el carrito | Deja al usuario sin carrito activo |
| D-28 | Products caído → 500 con CRT-005 | Es una falla de infraestructura, no del negocio |

**Users.API y Notifications.API**

| # | Decisión | En una línea |
|---|---|---|
| D-29 | Users caído → 500 con NTF-004 | Mismo criterio que D-28 |
| D-30 | El GET de notificaciones no consulta a Users | NTF-001 es solo para el POST |
| D-31 | Se notifica a usuarios bloqueados | El bloqueo impide el login, no recibir avisos |
| D-32 | Email con `[RegularExpression]` | Un email vacío da un solo error |

**Orders.API**

| # | Decisión | En una línea |
|---|---|---|
| D-33 | `FechaActualizacion` en `Order` | La respuesta de `PUT /status` la incluye |
| D-34 | Items repetidos se unen | El stock se valida contra la suma, como D-13 |
| D-35 | Un usuario bloqueado puede comprar | Mismo criterio que D-31 |
| D-36 | Users o Products caídos → 500 con ORD-007 | Mismo criterio que D-28 y D-29 |
| D-37 | Filtro que no es GUID → `[]` | El listado solo admite 200 y 500 |
| D-38 | El POST no devuelve 409 | Ningún código del catálogo lo produce al crear |

**Observabilidad y documentación**

| # | Decisión | En una línea |
|---|---|---|
| D-20 | Validar el `X-Correlation-Id` recibido | Evitar texto arbitrario en logs y en otros servicios |
| D-21 | Serilog sin logger estático | Evitar que los logs se pierdan entre APIs |
| D-22 | Swagger en todos los entornos | El enunciado lo pide y la demo se hace ahí |
| D-23 | `[ProducesError]` propio | No hay paquete de ejemplos compatible con Swashbuckle 10 |
| D-24 | `live` vs. `ready` | Separar "vivo" de "listo para atender" |

---

## 14. Próximos pasos

**Juan Pablo (Bloque 6, Etapa 9 en Orders y Notifications):**

1. **Confirmar con Thomas el formato de `GET /api/orders?productoId=` (D-07, D-37)** que va a leer su `OrdersClient`, y si Orders tiene una orden semilla con la Notebook para mostrar PRD-004.
2. `CorrelationIdDelegatingHandler` en los clientes de Orders y Notifications.
3. `DownstreamServiceHealthCheck`: Users y Products en el `/health/ready` de Orders, y Users en el de Notifications.
4. Prueba con los servicios levantados juntos. Los timeouts ya están (5 segundos).

**Thomas (Bloque 5, Etapa 9 en Products y Cart):**

1. ~~PR de `develop` a `main` para cerrar el **hito H2**~~ (PR #2, mergeado el 08/10).
2. ~~Products: `OrdersClient` real para PRD-004, Correlation ID saliente, Orders en `/health/ready` y timeout~~ (Etapa 9, parte 1). El formato de `GET /api/orders?productoId=` coincidió con el contrato; el `OrdersClient` solo lee `id` y `estado`.
3. Cart: `CorrelationIdDelegatingHandler`, Products en `/health/ready` y timeout de `ProductsClient` (Etapa 9, parte 2).

**Después:** las capturas de Swagger y la parte del README que falta (Etapa 10), y el ensayo de la defensa (Etapa 11).

---

## 15. Preguntas generales de la defensa

Las preguntas específicas de cada API están en su repaso: [Products](repaso-products-api.md#10-preguntas-probables-de-la-defensa), [Cart](repaso-cart-api.md#11-preguntas-probables-de-la-defensa), [Users](repaso-users-api.md#11-preguntas-probables-de-la-defensa), [Notifications](repaso-notifications-api.md#11-preguntas-probables-de-la-defensa) y [Orders](repaso-orders-api.md#11-preguntas-probables-de-la-defensa). Estas son las del proyecto en general.

**¿Por qué microservicios y no una sola API?**
Lo pide el enunciado, y además permite que cada servicio se desarrolle, pruebe y despliegue por separado. El costo es la comunicación por HTTP entre servicios, con sus fallas posibles. Para eso están los health checks, los timeouts y el Correlation ID.

**¿Cómo se comunican los servicios?**
Por HTTP, con `IHttpClientFactory`, siempre detrás de una interfaz y llevando el `X-Correlation-Id`. Ningún servicio lee los datos de otro directamente.

**¿Qué pasa si se cae un servicio del que dependen?**
Fallan solo las operaciones que lo necesitan, con el 500 del servicio y el error en el log. Por ejemplo, si Products se cae, Cart no puede agregar productos (CRT-005), pero sigue mostrando los carritos. Lo verificamos con los dos servicios levantados ([repaso de Cart, sección 7](repaso-cart-api.md#7-la-prueba-con-los-dos-servicios-levantados)).

**¿Por qué cada servicio tiene su propia copia de las excepciones y los handlers?**
Para que sean independientes (D-05). Una librería compartida obligaría a coordinar los cinco servicios ante cada cambio.

**¿Qué hacen si la librería de persistencia no llega a tiempo?**
El sistema funciona igual con los repositorios en memoria. Cuando llegue, se implementa la interfaz del repositorio con la librería y se cambia una línea de registro en cada servicio.

**¿Cómo se organizaron como equipo?**
Por servicios: cada uno es dueño de los suyos y los contratos entre servicios se acordaron antes de empezar. Trabajamos en paralelo sobre `develop` con commits pequeños, un CI que corre todos los tests y revisiones cruzadas del código del otro.

**¿Qué aprendieron de las revisiones de código?**
Que cada tipo de test detecta un tipo de error distinto. Los tests unitarios no detectaron dependencias sin registrar ni un repositorio con el ciclo de vida equivocado. Por eso cada API tiene un test que levanta la app real y verifica el contenedor.

**¿Cómo resolvieron lo que el enunciado no define?**
Con decisiones explícitas y documentadas: 38 en el registro del plan, cada una con su motivo. Las que agregan endpoints (D-06, D-07) o se apartan de la tabla del enunciado (D-38) quedaron marcadas para consultarlas con los docentes.

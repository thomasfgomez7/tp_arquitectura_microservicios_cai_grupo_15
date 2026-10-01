# Plan de desarrollo — E-Commerce con microservicios (.NET 10)

Trabajo práctico de Construcción de Aplicaciones Informáticas — Grupo 15.

Integrantes: **Thomas** y **Juan Pablo**.

Consignas del trabajo: [`TP_Microservicios_ECommerce_v7.md`](TP_Microservicios_ECommerce_v7.md) (versión en Markdown del [`.docx` original](TP_Microservicios_ECommerce_v7.docx) de la cátedra). Las referencias a "el enunciado" en este documento remiten a ese archivo.

Este documento es la guía de desarrollo del equipo: describe las decisiones tomadas, la forma de trabajo, el reparto de tareas para avanzar en paralelo y las etapas con sus tareas. Las casillas se marcan a medida que avanzamos.

---

## 1. Resumen del sistema

Cinco microservicios independientes, cada uno con su propia REST API:

| Servicio | Responsabilidad | Consume a | Responsable |
|---|---|---|---|
| Products.API | Catálogo de productos (CRUD) | Orders.API (PRD-004) | Thomas |
| Users.API | Registro, login y bloqueo de usuarios | — | Juan Pablo |
| Cart.API | Carrito de compras por usuario | Products.API | Thomas |
| Orders.API | Órdenes y su ciclo de estados | Users.API, Products.API | Juan Pablo |
| Notifications.API | Registro y envío simulado de notificaciones | Users.API | Juan Pablo |

Aspectos transversales en **todos** los servicios: contrato de errores con `errorCode` / `errorMessage`, manejo global con `IExceptionHandler`, Swagger con ejemplos, logs estructurados con Serilog, Correlation ID y Health Checks.

### Puertos

| Servicio | URL |
|---|---|
| Products.API | http://localhost:5001 |
| Users.API | http://localhost:5002 |
| Orders.API | http://localhost:5003 |
| Cart.API | http://localhost:5004 |
| Notifications.API | http://localhost:5005 |

---

## 2. Estructura objetivo

Los archivos de `MiniApi` (provistos por la cátedra) son la guía para cada API: `.csproj`, `Program.cs`, `appsettings.json`, `Properties/launchSettings.json` y el archivo `.http`. Se adaptan a lo que pide el enunciado.

### 2.1 Repositorio

```
ECommerce.slnx
├── src/
│   ├── Products.API/
│   ├── Users.API/
│   ├── Orders.API/
│   ├── Cart.API/
│   └── Notifications.API/
├── tests/
│   ├── Products.API.Tests/
│   ├── Users.API.Tests/
│   ├── Orders.API.Tests/
│   ├── Cart.API.Tests/
│   └── Notifications.API.Tests/
├── docs/
└── README.md
```

### 2.2 Carpetas de cada API

Ejemplo con Products.API. Cada interfaz vive en la misma carpeta que su implementación.

```
Products.API/
├── Controllers/                          # Traducen HTTP <-> DTO y delegan al servicio
│   └── ProductsController.cs
├── Models/                               # Entidades del dominio
│   └── Product.cs
├── DTOs/                                 # Request y Response DTOs
│   ├── CreateProductRequest.cs
│   ├── UpdateProductRequest.cs
│   ├── ProductResponse.cs
│   └── ErrorResponse.cs                  # contrato de error (sección 3.1), usado también por Swagger
├── Services/                             # Lógica de negocio
│   ├── IProductService.cs                # interfaz
│   └── ProductService.cs                 # implementación
├── Repositories/                         # Persistencia
│   ├── IProductRepository.cs             # interfaz
│   ├── InMemoryProductRepository.cs      # implementación (hasta tener la librería de la cátedra)
│   └── ProductSeedData.cs                # productos precargados para la demo
├── Clients/                              # Llamadas a otros microservicios
│   ├── IOrdersClient.cs                  # interfaz
│   ├── StubOrdersClient.cs               # implementación provisoria (se elimina en la Etapa 9)
│   ├── OrdersClient.cs                   # implementación HTTP (Etapa 9)
│   └── OrderInfo.cs                      # DTO con los datos que se leen de Orders.API
├── Exceptions/
│   ├── ErrorCodes.cs
│   ├── NotFoundException.cs
│   ├── BusinessRuleException.cs
│   └── ValidationException.cs
├── ExceptionHandlers/
│   ├── ErrorResponseWriter.cs            # arma el JSON de error del contrato
│   ├── NotFoundExceptionHandler.cs       # implementa IExceptionHandler
│   ├── BusinessRuleExceptionHandler.cs   # implementa IExceptionHandler
│   ├── ValidationExceptionHandler.cs     # implementa IExceptionHandler
│   ├── GlobalExceptionHandler.cs         # implementa IExceptionHandler
│   ├── ErrorHandlingOptions.cs           # nivel de detalle por entorno (appsettings)
│   └── ModelStateErrorMessage.cs         # errores de validación → errorMessage "A; B; C."
├── Infrastructure/
│   ├── ICorrelationIdAccessor.cs         # interfaz
│   ├── CorrelationIdAccessor.cs          # implementación
│   ├── CorrelationIdMiddleware.cs        # toma o genera X-Correlation-Id
│   ├── RequestLoggingMiddleware.cs       # log de inicio y fin con duración
│   ├── HttpContextItemKeys.cs            # claves compartidas en HttpContext.Items
│   ├── LoggingExtensions.cs              # configuración de Serilog (consola + archivo JSON)
│   ├── CorrelationIdDelegatingHandler.cs # hereda de DelegatingHandler (Etapa 9)
│   ├── DownstreamServiceHealthCheck.cs   # implementa IHealthCheck (Etapa 9)
│   ├── HealthCheckResponseWriter.cs      # respuesta JSON de /health
│   ├── HealthCheckExtensions.cs          # registra checks y mapea /health, /health/ready y /health/live
│   ├── ProductRepositoryHealthCheck.cs   # implementa IHealthCheck: la persistencia responde (ready)
│   ├── SwaggerExtensions.cs              # Swashbuckle + XML comments
│   ├── ProducesErrorAttribute.cs         # [ProducesError(404, PRD_001, ...)] documenta un error del catálogo
│   ├── ErrorExamplesOperationFilter.cs   # implementa IOperationFilter: ejemplo JSON por cada errorCode
│   └── ServiceCollectionExtensions.cs    # único lugar que asocia interfaces con implementaciones
├── logs/                                 # Generado por Serilog (ignorado por git)
├── Properties/launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── Products.API.http                     # Requests de prueba manual (éxito y error)
└── Program.cs
```

### 2.3 Interfaces e implementaciones

#### Criterio

Llevan **interfaz propia**:

- Los **servicios de negocio**: el controller depende de `IProductService`, no de `ProductService`.
- Todo lo que **sale del proceso**: la persistencia (repositorios) y los otros microservicios (clientes HTTP). Así se reemplazan en los tests y, cuando llegue la librería de la cátedra, solo cambia la implementación.
- Lo que **depende del entorno**: el acceso al Correlation ID del request actual y el envío de notificaciones.

**No** llevan interfaz propia:

- Models, DTOs, excepciones y `ErrorCodes`: son datos, no comportamiento.
- Controllers y middlewares: los crea y usa el framework.
- Reglas de dominio puras, sin dependencias externas (`AccountLockoutPolicy`, `OrderStatusTransitions`): se prueban directamente y una interfaz no aporta nada.
- Clases auxiliares sin estado (`ErrorResponseWriter`, `HealthCheckResponseWriter`).

Cuando el framework ya define la interfaz o la clase base, se implementa esa y no se crea una propia: `IExceptionHandler`, `IHealthCheck`, `DelegatingHandler`, `IPasswordHasher<T>` y `TimeProvider`.

#### Convenciones

- Prefijo `I` en las interfaces. Un archivo por tipo, en la misma carpeta que su implementación.
- Prefijo `InMemory` para los repositorios en memoria, sufijo `Client` para los clientes HTTP, prefijos `Stub` y `Simulated` para implementaciones provisorias o simuladas.
- Los DTOs que representan datos de **otro** servicio (`ProductInfo`, `UserInfo`, `OrderInfo`) van en `Clients/`, junto al cliente que los usa. Cada servicio tiene los suyos (D-05).
- Solo `Infrastructure/ServiceCollectionExtensions.cs` sabe qué implementación corresponde a cada interfaz. El resto del código depende únicamente de interfaces.

#### Componentes comunes a todas las APIs

Cada servicio tiene su propia copia (D-05).

| Interfaz o clase base | Implementación | Carpeta | Responsabilidad | Ciclo de vida | Etapa |
|---|---|---|---|---|---|
| `IExceptionHandler` (framework) | `NotFoundExceptionHandler`, `BusinessRuleExceptionHandler`, `ValidationExceptionHandler`, `GlobalExceptionHandler` | `ExceptionHandlers/` | Traducir cada tipo de excepción a la respuesta de error del contrato | Singleton | 2 |
| `ICorrelationIdAccessor` | `CorrelationIdAccessor` | `Infrastructure/` | Exponer el Correlation ID del request actual | Singleton ¹ | 3 |
| `TimeProvider` (framework) | `TimeProvider.System` (en tests: `FakeTimeProvider`) | — | Fecha y hora actual | Singleton | 1 |
| `DelegatingHandler` (framework) | `CorrelationIdDelegatingHandler` ² | `Infrastructure/` | Agregar `X-Correlation-Id` a las llamadas salientes | Transient | 9 |
| `IHealthCheck` (framework) | `DownstreamServiceHealthCheck` ² | `Infrastructure/` | Verificar que responda un servicio del que se depende | Singleton | 9 |

¹ Se apoya en `IHttpContextAccessor` para funcionar también dentro de los `DelegatingHandler`, que `IHttpClientFactory` crea en un scope propio.
² Solo en los servicios que consumen a otros: Products, Cart, Orders y Notifications.

Clases concretas comunes (sin interfaz): `ErrorCodes`, `NotFoundException`, `BusinessRuleException`, `ValidationException`, `ErrorResponseWriter`, `CorrelationIdMiddleware`, `RequestLoggingMiddleware`, `HealthCheckResponseWriter` y `ServiceCollectionExtensions`.

#### Products.API — Thomas

| Interfaz | Implementación | Carpeta | Responsabilidad | Ciclo de vida | Etapa |
|---|---|---|---|---|---|
| `IProductService` | `ProductService` | `Services/` | Reglas de negocio de productos (PRD-001 a PRD-004) | Scoped | 1 |
| `IProductRepository` | `InMemoryProductRepository` → adaptador de la librería de la cátedra | `Repositories/` | Guardar y consultar productos | Singleton | 1 |
| `IOrdersClient` | `StubOrdersClient` (provisoria) → `OrdersClient` | `Clients/` | Saber si un producto tiene órdenes activas | Singleton (stub) · typed client (HTTP) | 1 y 9 |

Clases concretas: `ProductsController`, `Product`, `CreateProductRequest`, `UpdateProductRequest`, `ProductResponse`, `ProductSeedData` (`Repositories/`, datos de la demo con IDs fijos), `OrderInfo`.

#### Users.API — Juan Pablo

| Interfaz | Implementación | Carpeta | Responsabilidad | Ciclo de vida | Etapa |
|---|---|---|---|---|---|
| `IUserService` | `UserService` | `Services/` | Registro, login y consulta por ID (USR-001 a USR-007) | Scoped | 5 |
| `IUserRepository` | `InMemoryUserRepository` → adaptador de la librería de la cátedra | `Repositories/` | Guardar y consultar usuarios | Singleton | 5 |
| `IPasswordHasher<User>` (framework) | `PasswordHasher<User>` (framework) | — | Hashear y verificar contraseñas | Singleton | 5 |

Clases concretas: `UsersController`, `User`, `RegisterUserRequest`, `LoginRequest`, `UserResponse`, `LoginResponse`, `AccountLockoutPolicy` (`Services/`, regla de bloqueo).

#### Cart.API — Thomas

| Interfaz | Implementación | Carpeta | Responsabilidad | Ciclo de vida | Etapa |
|---|---|---|---|---|---|
| `ICartService` | `CartService` | `Services/` | Reglas del carrito (CRT-001 a CRT-004) | Scoped | 6 |
| `ICartRepository` | `InMemoryCartRepository` → adaptador de la librería de la cátedra | `Repositories/` | Guardar y consultar carritos | Singleton | 6 |
| `IProductsClient` | `ProductsClient` | `Clients/` | Consultar existencia y stock en Products.API | Typed client | 6 |

Clases concretas: `CartController`, `Cart`, `CartItem`, `AddCartItemRequest`, `UpdateCartItemRequest`, `CartResponse`, `CartItemResponse`, `ProductInfo`.

#### Orders.API — Juan Pablo

| Interfaz | Implementación | Carpeta | Responsabilidad | Ciclo de vida | Etapa |
|---|---|---|---|---|---|
| `IOrderService` | `OrderService` | `Services/` | Creación, consulta y cambio de estado (ORD-001 a ORD-006) | Scoped | 7 |
| `IOrderRepository` | `InMemoryOrderRepository` → adaptador de la librería de la cátedra | `Repositories/` | Guardar y consultar órdenes | Singleton | 7 |
| `IUsersClient` | `UsersClient` | `Clients/` | Verificar que el usuario exista en Users.API | Typed client | 7 |
| `IProductsClient` | `ProductsClient` | `Clients/` | Consultar existencia, precio y stock en Products.API | Typed client | 7 |

Clases concretas: `OrdersController`, `Order`, `OrderItem`, `OrderStatus` (enum), `CreateOrderRequest`, `CreateOrderItemRequest`, `UpdateOrderStatusRequest`, `OrderResponse`, `OrderItemResponse`, `OrderStatusResponse`, `OrderStatusTransitions` (`Services/`, máquina de estados), `UserInfo`, `ProductInfo`.

#### Notifications.API — Juan Pablo

| Interfaz | Implementación | Carpeta | Responsabilidad | Ciclo de vida | Etapa |
|---|---|---|---|---|---|
| `INotificationService` | `NotificationService` | `Services/` | Registrar, enviar y listar notificaciones (NTF-001 a NTF-003) | Scoped | 8 |
| `INotificationRepository` | `InMemoryNotificationRepository` → adaptador de la librería de la cátedra | `Repositories/` | Guardar y consultar notificaciones | Singleton | 8 |
| `INotificationSender` | `SimulatedNotificationSender` | `Services/` | Simular el envío; se puede cambiar por un envío real sin tocar el servicio | Singleton | 8 |
| `IUsersClient` | `UsersClient` | `Clients/` | Verificar que el usuario exista en Users.API | Typed client | 8 |

Clases concretas: `NotificationsController`, `Notification`, `NotificationType` (enum: Email, Push, SMS), `NotificationStatus` (enum: Pendiente, Enviada, Fallida), `SendNotificationRequest`, `NotificationResponse`, `UserInfo`.

#### Cómo se reemplazan en los tests

- **Tests unitarios:** las interfaces se reemplazan con dobles de NSubstitute y la hora con `FakeTimeProvider`. Los clientes HTTP se prueban con un `HttpMessageHandler` falso.
- **Tests de integración:** `WebApplicationFactory` con `ConfigureTestServices` para reemplazar los clientes HTTP por dobles. Los repositorios en memoria se usan tal cual.

---

## 3. Forma de trabajo

### Git

- Ambos integrantes trabajan directamente sobre la rama `develop`.
- Los commits se hacen con **Commitizen** (`cz commit`), con el formato Conventional Commits: `tipo(alcance): descripción`.
  - Tipos habituales: `feat`, `fix`, `test`, `refactor`, `docs`, `chore`.
  - Alcance: el servicio (`products`, `users`, `cart`, `orders`, `notifications`) o `solution` / `docs` para cambios generales.
- Al cerrar cada hito, Thomas abre un **Pull Request de `develop` a `main`**.
- **Nunca se mergea código que no compile.** Antes de cada push y de cada PR, `dotnet build` y `dotnet test` tienen que estar en verde.
- **Integración continua:** GitHub Actions (`.github/workflows/ci.yml`) compila la solución y ejecuta todos los tests en cada push a `develop` o `main` y en cada PR a `main`. Si el workflow falla, se arregla antes de seguir y el PR no se mergea.

### Hitos (PR `develop` → `main`)

| Hito | Al terminar | Resultado |
|---|---|---|
| H1 | Bloque 1 | Solución, proyectos y tests compilando |
| H2 | Bloque 3 | Products.API completo (plantilla) y Users.API con su contrato de errores |
| H3 | Bloque 5 | Los cinco servicios funcionando |
| H4 | Bloque 7 | Integración entre servicios, documentación y entrega |

Los bloques se describen en la sección 4.2.

### TDD

Cada funcionalidad se desarrolla con el ciclo **rojo → verde → refactor**:

1. **Rojo:** escribir un test que describa el comportamiento esperado (por ejemplo, "crear un producto duplicado lanza PRD-003") y verlo fallar.
2. **Verde:** escribir el código mínimo para que pase.
3. **Refactor:** mejorar el diseño con los tests como red de seguridad.

Dos niveles de tests por servicio, cada uno en su carpeta dentro del proyecto de tests:

- **`Unit/`:** servicios, reglas de negocio y clientes HTTP. Las dependencias se reemplazan por dobles de prueba (ver 2.3).
- **`Integration/`:** la API completa levantada en memoria con `WebApplicationFactory`. Verifican el contrato HTTP: status codes y el JSON exacto de las respuestas de error.

Herramientas: xUnit, `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory), NSubstitute (dobles de prueba) y `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`). Convención de nombres: `Metodo_Escenario_ResultadoEsperado`.

### Principios de diseño: alta cohesión, bajo acoplamiento

- **Controllers delgados:** solo reciben el DTO, llaman al servicio y devuelven el status. Sin lógica de negocio ni `try/catch`.
- **Services con una responsabilidad:** contienen la lógica de negocio de un único recurso y dependen solo de interfaces (ver 2.3).
- **Reglas complejas en su propia clase:** la máquina de estados de las órdenes (`OrderStatusTransitions`) y la política de bloqueo de usuarios (`AccountLockoutPolicy`).
- **Repositorios como única puerta a la persistencia:** cuando llegue la librería de la cátedra solo cambia la implementación, no los servicios.
- **Clientes HTTP detrás de interfaces:** el servicio no sabe que del otro lado hay una llamada HTTP.
- **DTOs separados de las entidades:** la API nunca expone un modelo del dominio (así, por ejemplo, `PasswordHash` nunca sale).
- **Un ExceptionHandler por tipo de excepción.**
- **`Program.cs` ordenado:** el registro de dependencias se agrupa en `ServiceCollectionExtensions` para que no crezca sin control.
- **Sin código compartido entre microservicios:** cada servicio es autónomo; la plantilla de Products.API se replica.
- **Fechas con `TimeProvider`** (incluido en .NET) para poder testear lo que depende de la hora actual.

Los diagramas de arquitectura y de clases están en [`docs/arquitectura.md`](arquitectura.md).

### Convenciones de código

Reglas concretas que surgieron de las revisiones. La referencia de cómo aplicarlas es Products.API.

| Tema | Convención | Por qué |
|---|---|---|
| Archivos | Un tipo por archivo, siempre con extensión `.cs`, creado desde el editor ("New Class"). Antes de cada commit, revisar `git status`. | Un archivo sin `.cs` no se compila y el CI no lo detecta. |
| Nombres | Clases y métodos en inglés (`GetByIdAsync`, `CreateAsync`); propiedades del dominio en español, como el contrato (`Nombre`, `Email`, `IntentosFallidos`). | Coherencia entre servicios; el JSON queda igual al del enunciado. |
| Asincronía | Todo método de servicio, repositorio o cliente es `async`, termina en `Async` y recibe `CancellationToken cancellationToken = default`. | La persistencia real y las llamadas HTTP lo necesitan. |
| DTOs | `record` con propiedades `init`. Los requests llevan Data Annotations con `ErrorMessage` en español. Campos numéricos obligatorios como `int?` / `decimal?` con `[Required]`. | Inmutables; el `errorMessage` de 400 sale en español; un campo faltante da 400 y no un 0 silencioso. |
| Modelos | Sin valores calculados en los defaults: `Id` y fechas los asigna el servicio (`Guid.NewGuid()`, `TimeProvider`). | Se puede testear la fecha exacta. |
| Excepciones | Exactamente la forma de la sección 4.4: `NotFoundException(errorCode, message)`, `BusinessRuleException(errorCode, message, statusCode)`, `ValidationException(errorCode, message)`. | Los handlers son iguales en todos los servicios. |
| Status HTTP | `StatusCodes.Status409Conflict` y similares, nunca números sueltos. Las constantes de negocio (por ejemplo, el máximo de intentos) viven en un solo lugar. | Se lee la intención y no hay valores duplicados. |
| Repositorios en memoria | Registrados como **Singleton**, con `ConcurrentDictionary`, devolviendo listas ya materializadas (`ToList()`). `UpdateAsync` guarda de verdad el objeto. | Scoped pierde los datos entre requests; `List<T>` falla con requests simultáneos. |
| Inyección de dependencias | Todo se registra en `Infrastructure/ServiceCollectionExtensions.cs` (`AddXxxServices()`), llamado desde `Program.cs`. Cada API tiene un `DependencyInjectionTests`. | Los tests unitarios crean el servicio a mano y no detectan registros faltantes. |
| Tests | `Unit/Services/`, `Unit/Repositories/`, `Integration/`. Nombre `Metodo_Escenario_ResultadoEsperado`. Se verifica `ErrorCode`, `StatusCode` y `Message` de cada excepción. | Mismo formato en todos los servicios; se prueba el contrato completo. |
| Contratos entre servicios | Los endpoints y códigos de la sección 4.4 (y 5.1) no se quitan ni se cambian sin acordarlo y actualizar el plan. | El servicio que rompe el contrato sigue en verde; el que falla es el otro. |
| Commits | Cada commit compila, pasa los tests y no incluye archivos vacíos ni código comentado. | Todo lo que está en `develop` tiene que funcionar. |

---

## 4. Reparto de tareas: Thomas y Juan Pablo

### 4.1 Responsabilidades

| | Thomas | Juan Pablo |
|---|---|---|
| Servicios | Products.API, Cart.API | Users.API, Notifications.API, Orders.API |
| Transversales | Esqueleto de la solución (Etapa 0) y plantilla: contrato de errores, Serilog, Correlation ID, Swagger y Health Checks (Etapas 2 a 4) | Replicar la plantilla en sus tres servicios |
| Integración (Etapa 9) | Propagación de Correlation ID y `/health/ready` en Products y Cart; `OrdersClient` real para PRD-004 | Propagación de Correlation ID y `/health/ready` en Orders y Notifications |
| Documentación (Etapa 10) | README: ejecución, puertos y diagrama; script de arranque; capturas de Swagger | README: tabla de códigos de error y decisiones de diseño |
| Pull Requests a `main` | Sí | — |

**Por qué este reparto:**

- Cada uno es dueño de servicios que consumen a otro servicio propio: Cart consume a Products (Thomas); Notifications y Orders consumen a Users (Juan Pablo). Así cada uno conoce bien los contratos que usa.
- Thomas construye la plantilla transversal, que es la parte más pesada; Juan Pablo tiene más servicios, pero los arma replicando esa plantilla.
- Cada uno trabaja en sus propias carpetas, por lo que casi no hay conflictos en `develop`.

### 4.2 Plan en bloques paralelos

Cada bloque se trabaja en paralelo. Al final de cada bloque hay un punto de sincronización: los dos bajan los cambios del otro, verifican que todo compile y revisan el código del compañero (ver 4.5).

| Bloque | Thomas | Juan Pablo | Sincronización |
|---|---|---|---|
| 1 | Etapa 0: esqueleto de la solución con los 10 proyectos. | Leer el enunciado y este plan; bajar `develop` y verificar que compila y que los tests corren. | Juntos: acordar los contratos de la sección 4.4. **Hito H1.** |
| 2 | Etapa 1: Products, dominio y servicio. Etapa 2: endpoints y contrato de errores. | Etapa 5: Users, lógica de negocio (registro, login, bloqueo y búsqueda por ID) con tests unitarios. | Thomas avisa que el contrato de errores de Products está listo para replicar. |
| 3 | Etapa 3: Serilog y Correlation ID. Etapa 4: Swagger y Health Checks. | Etapa 5: Users, capa HTTP y contrato de errores (replicando Products). Etapa 8: Notifications, lógica de negocio. | Plantilla completa. **Hito H2.** |
| 4 | Etapa 6: Cart completo. | Etapas 5 y 8: agregar a Users y Notifications los transversales de la plantilla; terminar Notifications. | Users, Notifications y Cart completos. |
| 5 | Etapa 9: Correlation ID saliente y `/health/ready` en Products y Cart; `OrdersClient` real para PRD-004 (con tests contra el contrato acordado). | Etapa 7: Orders completo. | Orders completo. **Hito H3.** |
| 6 | Etapa 10: README (ejecución, puertos, diagrama), script de arranque y capturas de Swagger. | Etapa 9 en Orders y Notifications. Etapa 10: tabla de códigos de error y decisiones de diseño en el README. | Integración de punta a punta funcionando. |
| 7 | Etapa 11 (juntos): prueba integral, ensayo de la demo y revisión cruzada. | Etapa 11 (juntos). | **Hito H4: entrega.** |

### 4.3 Reglas para trabajar en paralelo sobre `develop`

- Cada uno trabaja solo en las carpetas de sus servicios (`src/<Servicio>.API` y `tests/<Servicio>.API.Tests`). Si hace falta tocar algo del otro, se avisa antes.
- `ECommerce.slnx` se crea en la Etapa 0 con todos los proyectos, para que nadie tenga que modificarlo después.
- Archivos compartidos (`README.md`, `.gitignore`, `docs/`): se avisa antes de modificarlos y se hacen commits chicos.
- `git pull --rebase` antes de empezar a trabajar y antes de cada push.
- Commits chicos y push frecuente (como mínimo al terminar cada sesión de trabajo), siempre con build y tests en verde.
- Si algo rompe el build en `develop`, se avisa enseguida y quien lo rompió lo arregla antes de seguir.

### 4.4 Contratos acordados antes de empezar

Para que cada uno pueda avanzar sin esperar al otro, estos contratos se fijan en el Bloque 1 y no se cambian sin avisar.

**Excepciones y códigos de error** (misma forma en todos los servicios, cada uno con su propia copia):

- `NotFoundException(errorCode, message)` → 404.
- `BusinessRuleException(errorCode, message, statusCode)` → 401, 403, 409 o 422 según el catálogo (D-10).
- `ValidationException(errorCode, message)` → 400.
- Clase `ErrorCodes` con constantes por servicio: `PRD_001`, `USR_001`, `ORD_001`, `CRT_001`, `NTF_001`, …

**Formato de error:** el de la sección 3.1 del enunciado, más el campo `correlationId` (D-11).

**Interfaces de los clientes HTTP:** los nombres de la sección 2.3 (`IProductsClient`, `IUsersClient`, `IOrdersClient`).

**Endpoints que se consumen entre servicios:**

| Consumidor | Endpoint (servicio) | Respuesta que se usa |
|---|---|---|
| Cart, Orders | `GET /api/products/{id}` (Products) | 200 con `id`, `nombre`, `precio` y `stock`; 404 si no existe |
| Orders, Notifications | `GET /api/users/{id}` (Users) | 200 con `id`, `nombre`, `apellido`, `email`, `fechaRegistro` y `activo`; 404 (USR-007) si no existe |
| Products | `GET /api/orders?productoId={id}` (Orders) | 200 con la lista de órdenes que incluyen ese producto; se revisa `estado` |

**Header:** `X-Correlation-Id` en todas las llamadas entre servicios.

**Puertos:** los de la sección 1.

### 4.5 Revisión cruzada

El enunciado exige que cada integrante pueda explicar cualquier parte del código. Por eso:

- En cada punto de sincronización, cada uno lee los commits del otro desde el bloque anterior y anota dudas para charlarlas.
- Antes de la defensa (Bloque 7), cada uno explica en voz alta un servicio del otro.

---

## 5. Registro de decisiones

Decisiones propias ante puntos que el enunciado no define. Se documentan también en el README final.

| # | Decisión | Motivo |
|---|---|---|
| D-01 | Solución `ECommerce.slnx` con `src/*.API` y `tests/*.Tests`, usando `MiniApi` como guía. | Sección 6 del enunciado + TDD. |
| D-02 | Controllers en lugar de Minimal API. | La sección 6 pide la carpeta `Controllers/` y XML comments en controladores. |
| D-03 | Persistencia en memoria detrás de interfaces de repositorio hasta recibir la librería de la cátedra. | Permite avanzar sin bloquearse y cambiar la implementación sin tocar servicios. |
| D-04 | `appsettings.Development.json` se versiona (se quita del `.gitignore`). | El enunciado pide controlar el nivel de detalle de errores por entorno; no contiene secretos. |
| D-05 | Sin librería compartida entre microservicios. | Autonomía de cada servicio; bajo acoplamiento. |
| D-06 | Endpoint adicional `GET /api/users/{id}` en Users.API, con el nuevo código `USR-007` (ver 5.1). | Orders (ORD-003) y Notifications (NTF-001) necesitan verificar que el usuario exista. |
| D-07 | Filtro adicional `?productoId=` en `GET /api/orders`. | Products necesita saber si hay órdenes activas de un producto (PRD-004). |
| D-08 | `Activo=false` con `IntentosFallidos >= 3` → USR-004; `Activo=false` con menos intentos → USR-005 (bloqueo manual). | El modelo no tiene un campo "motivo de bloqueo". |
| D-09 | El tercer intento fallido responde 401 (USR-003) y bloquea la cuenta; los siguientes intentos responden 403 (USR-004), incluso con la contraseña correcta. | La respuesta refleja el motivo de ese intento; el bloqueo aplica a partir del siguiente. |
| D-10 | `BusinessRuleException` lleva el status HTTP como propiedad. | El catálogo usa reglas de negocio con 401, 403, 409 y 422. |
| D-11 | Las respuestas de error incluyen un campo extra `correlationId`. | Requisito 5.5. |
| D-12 | Cada endpoint devuelve únicamente los status listados en su contrato (por ejemplo, `PUT /api/products/{id}` no devuelve 409). | Respetar el contrato de la API. |
| D-13 | Cart: agregar un producto que ya está en el carrito suma la cantidad; el stock se valida contra el total. | Comportamiento esperable de un carrito. |
| D-14 | Crear una orden **no** descuenta stock (mejora opcional). | El enunciado no lo exige. |
| D-15 | Interfaces solo donde aportan desacoplamiento: servicios, persistencia, otros microservicios y dependencias del entorno (criterio de la sección 2.3). | Bajo acoplamiento sin interfaces innecesarias. |
| D-16 | Los servicios exponen solo HTTP en desarrollo: sin perfil `https` en `launchSettings.json` ni `UseHttpsRedirection`. | Simplifica las llamadas entre servicios en local (sin certificados de desarrollo ni redirecciones). |
| D-17 | Los ids de ruta se reciben como texto (`{id}`, no `{id:guid}`) y el controller los convierte; si no es un GUID válido responde 404 con el código "no encontrado" del servicio. | El enunciado muestra `GET /api/products/99` → 404 PRD-001. Con `{id:guid}` el ruteo devuelve un 404 vacío, sin `errorCode`. |
| D-18 | Errores inesperados (500): en `appsettings.Development.json` (`ErrorHandling:IncludeExceptionDetails = true`) el `detail` incluye el mensaje de la excepción; en producción, un texto genérico. Nunca se expone el stack trace. | Requisito 5.2: controlar el nivel de detalle por entorno. |
| D-19 | Un body que no es JSON válido responde PRD-002 con el mensaje único "El cuerpo de la solicitud no es un JSON válido."; los errores de Data Annotations se unen como "A; B; C.". | Los mensajes de .NET para JSON inválido están en inglés; el enunciado pide listar los problemas separados por punto y coma. |
| D-20 | Un `X-Correlation-Id` recibido solo se acepta si tiene hasta 64 caracteres de `[A-Za-z0-9._-]`; si no, se genera uno nuevo (GUID). | El valor viene del cliente y termina escrito en logs y en otros servicios: se evita inyectar texto arbitrario. |
| D-21 | Serilog se registra con `services.AddSerilog(preserveStaticLogger: true, ...)`, sin usar `Log.Logger` estático. El archivo JSON se desactiva en los tests (`LogFile:Enabled = false`). | Con el logger estático, cuando los tests levantan varias APIs en paralelo, la última reemplaza el logger de las demás y los logs se pierden. |
| D-22 | Swagger UI queda habilitado en todos los entornos, en `/swagger`. | El enunciado lo pide en cada microservicio y la demo se hace desde ahí. En un sistema real se restringiría a desarrollo. |
| D-23 | Los errores se documentan con `[ProducesError(status, código, mensaje)]`, que hereda de `ProducesResponseType`, y un `IOperationFilter` arma el ejemplo con `ErrorResponseWriter.Build`. No se usa `Swashbuckle.AspNetCore.Filters`. | Ese paquete no tiene versión para Swashbuckle 10. Además, el ejemplo de Swagger sale del mismo código que arma las respuestas reales. |
| D-24 | `/health/live` solo verifica que el proceso responda; `/health/ready` verifica la persistencia (y desde la Etapa 9, los servicios de los que depende). Healthy y Degraded responden 200; Unhealthy, 503. | Separar "vivo" de "listo para atender" es la convención de los health checks: un servicio puede estar vivo con una dependencia caída. |

### 5.1 Códigos de error agregados al catálogo

| errorCode | HTTP | errorMessage | Cuándo se devuelve |
|---|---|---|---|
| USR-007 | 404 | Usuario no encontrado. | `GET /api/users/{id}` cuando el ID no existe. |

Ejemplo de respuesta:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "El recurso solicitado no fue encontrado.",
  "instance": "/api/users/a1b2c3d4-0000-0000-0000-111122223333",
  "errorCode": "USR-007",
  "errorMessage": "Usuario no encontrado.",
  "correlationId": "0f8fad5b-d9cb-469f-a165-70867728950e"
}
```

Este código se documenta en Swagger y en la tabla de códigos de error del README junto con los del enunciado.

### 5.2 Pendientes

- [ ] Recibir la librería de persistencia de la cátedra y reemplazar los repositorios en memoria.
- [ ] Consultar con los docentes si están de acuerdo con los endpoints adicionales (D-06, D-07) y el código USR-007.

---

## 6. Etapas

En cada etapa, las tareas de código se separan en **clases concretas** (sin interfaz) e **interfaces → implementaciones**, siguiendo la sección 2.3.

### Etapa 0 — Entorno, esqueleto y base de testing

**Responsable:** Thomas · **Bloque:** 1

Objetivo: tener la solución compilando, con los cinco proyectos y sus proyectos de tests.

- [x] Instalar el SDK de .NET 10 (cada integrante) y verificar con `dotnet --version`.
- [x] Crear `ECommerce.slnx` (reemplaza a `MiniApi.slnx`).
- [x] Crear los cinco proyectos en `src/` tomando como guía los archivos de `MiniApi`, sin el ejemplo `WeatherForecast` y preparados para Controllers.
- [x] Asignar los puertos fijos en cada `launchSettings.json`, solo con perfil HTTP (D-16).
- [x] Agregar `appsettings.Development.json` a cada API y quitarlo del `.gitignore` (D-04).
- [x] Crear los cinco proyectos `tests/*.Tests` (xUnit), con carpetas `Unit/` e `Integration/`, y agregarlos a la solución.
- [x] Hacer visible la clase `Program` para los tests de integración. En .NET 10 no hace falta código: el framework genera `public partial class Program` automáticamente.
- [x] Un test de humo por API que levante la aplicación en memoria.
- [x] Eliminar el proyecto `MiniApi` y crear `docs/`.
- [x] (Opcional) GitHub Actions que ejecute `dotnet build` y `dotnet test` en cada push a `develop` y en cada PR a `main` (`.github/workflows/ci.yml`).
- [x] Juntos: acordar los contratos de la sección 4.4.

**Lista cuando:** `dotnet build ECommerce.slnx` y `dotnet test ECommerce.slnx` pasan en verde y cada servicio levanta en su puerto.

### Etapa 1 — Products.API: dominio y lógica de negocio

**Responsable:** Thomas · **Bloque:** 2

Objetivo: el servicio de productos con todas sus reglas, probado con tests unitarios.

Tests primero (unitarios de `ProductService`, con dobles de `IProductRepository` e `IOrdersClient`):
- [x] Crear un producto asigna `Id` y `FechaCreacion`.
- [x] Crear un duplicado (mismo nombre en la misma categoría, sin distinguir mayúsculas) lanza PRD-003.
- [x] Obtener, actualizar o eliminar un ID inexistente lanza PRD-001.
- [x] Eliminar un producto con órdenes activas lanza PRD-004.
- [x] Listar filtra por `categoria` y por `nombre`.

Clases concretas:
- [x] `Product` (Models) y `CreateProductRequest`, `UpdateProductRequest`, `ProductResponse` (DTOs) con Data Annotations (Apéndice A).
- [x] `ErrorCodes` (`PRD_001` … `PRD_005`), `NotFoundException`, `BusinessRuleException` (D-10) y `ValidationException`.

Interfaces → implementaciones:
- [x] `IProductRepository` → `InMemoryProductRepository`, con datos semilla para la demo (D-03).
- [x] `IOrdersClient` → `StubOrdersClient`: implementación provisoria que responde "sin órdenes activas" (se reemplaza por `OrdersClient` en la Etapa 9).
- [x] `IProductService` → `ProductService`.
- [x] Registro de las tres en `ServiceCollectionExtensions`, junto con `TimeProvider.System`.

**Lista cuando:** todos los tests unitarios de Products pasan.

### Etapa 2 — Products.API: endpoints y contrato de errores

**Responsable:** Thomas · **Bloque:** 2

Objetivo: exponer el servicio por HTTP respetando exactamente el contrato del enunciado.

Tests primero (integración con `WebApplicationFactory`):
- [x] Cada endpoint devuelve el status correcto en el caso exitoso (200, 201 con header `Location`, 204).
- [x] Cada error del catálogo PRD devuelve el JSON exacto del contrato (`type`, `title`, `status`, `detail`, `instance`, `errorCode`, `errorMessage`).
- [x] Un request inválido devuelve PRD-002 con los problemas separados por `;`.
- [x] Una excepción inesperada devuelve PRD-005 sin stack trace.

Clases concretas:
- [x] `ProductsController` con los cinco endpoints (depende de `IProductService`).
- [x] `ErrorResponseWriter`: arma y escribe el JSON de error del contrato; lo usan todos los handlers.
- [x] Redirigir la validación automática de `[ApiController]` a una `ValidationException` con PRD-002.
- [x] Nivel de detalle de errores según entorno (`appsettings.Development.json` vs producción).
- [x] `Products.API.http` con requests de éxito y de error.

Implementaciones de `IExceptionHandler` (framework):
- [x] `NotFoundExceptionHandler`, `BusinessRuleExceptionHandler`, `ValidationExceptionHandler` y `GlobalExceptionHandler`, registrados en ese orden (del más específico al más genérico), con `AddProblemDetails()` y `app.UseExceptionHandler()`.

**Lista cuando:** todos los errores del catálogo PRD están cubiertos por tests de integración en verde. Thomas avisa a Juan Pablo para que replique esta capa en Users.

### Etapa 3 — Products.API: Serilog y Correlation ID

**Responsable:** Thomas · **Bloque:** 3

Objetivo: observabilidad del servicio.

Tests primero:
- [x] Toda respuesta incluye el header `X-Correlation-Id`.
- [x] Si el request trae `X-Correlation-Id`, la respuesta devuelve el mismo valor.
- [x] Las respuestas de error incluyen el campo `correlationId` (D-11).

Clases concretas:
- [x] Configuración de Serilog, primera en `Program.cs`: consola en formato legible y archivo en JSON estructurado dentro de `logs/`.
- [x] `CorrelationIdMiddleware`: toma o genera el `X-Correlation-Id`, lo devuelve en la respuesta y lo agrega al contexto de logs.
- [x] `RequestLoggingMiddleware`: log de inicio y fin de cada request con su duración.
- [x] `ErrorResponseWriter` agrega el campo `correlationId`.
- [x] Los handlers loguean los errores de negocio como `Warning` y los inesperados como `Error`, con su `ErrorCode`.
- [x] Cada log incluye Timestamp, Nivel, Servicio, Endpoint, CorrelationId y ErrorCode cuando aplique.

Interfaces → implementaciones:
- [x] `ICorrelationIdAccessor` → `CorrelationIdAccessor`: expone el ID del request actual a los handlers y, desde la Etapa 9, al `CorrelationIdDelegatingHandler`.

**Lista cuando:** los tests pasan y un request con error deja en el archivo JSON una línea con todos los campos pedidos.

### Etapa 4 — Products.API: Swagger y Health Checks

**Responsable:** Thomas · **Bloque:** 3

Objetivo: cerrar Products.API como plantilla del resto de los servicios.

Tests primero:
- [x] `/swagger/v1/swagger.json` responde 200 y documenta todos los status de cada endpoint.
- [x] `/health`, `/health/ready` y `/health/live` responden JSON con `Healthy`, `Degraded` o `Unhealthy`.

Clases concretas:
- [x] Reemplazar `AddOpenApi` de la plantilla por Swashbuckle, con Swagger UI en `/swagger`.
- [x] XML comments en el controller y los DTOs; endpoints agrupados por tags.
- [x] Documentar cada status posible con `ProducesResponseType`, incluyendo ejemplos de éxito y de error con su `errorCode`.
- [x] `HealthCheckResponseWriter`: respuesta JSON de los health checks.

**Lista cuando:** Products.API cumple todos los requerimientos funcionales y no funcionales y queda lista como plantilla.

### Etapa 5 — Users.API

**Responsable:** Juan Pablo · **Bloques:** 2 (lógica), 3 (capa HTTP) y 4 (transversales)

Objetivo: registro y login con la política de bloqueo.

Tests primero:
- [ ] Registrar guarda la contraseña hasheada y la respuesta nunca incluye `PasswordHash`.
- [ ] Registrar un email existente (sin distinguir mayúsculas) lanza USR-001; datos inválidos, USR-002.
- [ ] Login correcto devuelve 200 y resetea `IntentosFallidos`.
- [ ] Login con email inexistente o contraseña incorrecta devuelve USR-003 e incrementa los intentos.
- [ ] `AccountLockoutPolicy`: el tercer intento fallido bloquea la cuenta (D-09).
- [ ] Usuario bloqueado por intentos devuelve USR-004; bloqueado manualmente, USR-005 (D-08).
- [ ] `GET /api/users/{id}` devuelve el usuario o USR-007 (D-06, 5.1).
- [ ] Contrato de errores completo (USR-001 a USR-007) en tests de integración.

Lógica de negocio (Bloque 2):

Clases concretas:
- [ ] `User` (Models) y `RegisterUserRequest`, `LoginRequest`, `UserResponse`, `LoginResponse` (DTOs).
- [ ] `ErrorCodes` (`USR_001` … `USR_007`) y excepciones según la sección 4.4.
- [ ] `AccountLockoutPolicy`: regla de bloqueo.

Interfaces → implementaciones:
- [ ] `IUserRepository` → `InMemoryUserRepository`, con datos semilla que incluyan un usuario bloqueado manualmente para demostrar USR-005.
- [ ] `IPasswordHasher<User>` → `PasswordHasher<User>` (ambas del framework).
- [ ] `IUserService` → `UserService`.

Capa HTTP y contrato de errores (Bloque 3, replicando la Etapa 2):
- [ ] `UsersController` con `register`, `login` y `GET /api/users/{id}`.
- [ ] `ErrorResponseWriter` y los cuatro `IExceptionHandler`; validación automática con USR-002.
- [ ] `Users.API.http` con requests de éxito y de error.

Transversales (Bloque 4, replicando las Etapas 3 y 4):
- [ ] `ICorrelationIdAccessor` → `CorrelationIdAccessor`, `CorrelationIdMiddleware`, `RequestLoggingMiddleware` y Serilog.
- [ ] Swagger y `HealthCheckResponseWriter`.

### Etapa 6 — Cart.API

**Responsable:** Thomas · **Bloque:** 4

Objetivo: primer servicio que consume a otro.

Tests primero:
- [ ] Agregar un producto inexistente lanza CRT-002; sin stock suficiente, CRT-003; cantidad ≤ 0, CRT-004.
- [ ] Operar sobre un usuario sin carrito lanza CRT-001.
- [ ] Agregar un producto que ya está en el carrito suma la cantidad (D-13).
- [ ] Actualizar cantidad, quitar un producto y vaciar el carrito.
- [ ] `ProductsClient` (con un `HttpMessageHandler` falso): 200 devuelve el producto, 404 devuelve "no existe", un error o timeout se traduce en CRT-005.
- [ ] Contrato de errores completo (CRT-001 a CRT-005) en tests de integración.

Lógica de negocio:

Clases concretas:
- [ ] `Cart`, `CartItem` (Models) y `AddCartItemRequest`, `UpdateCartItemRequest`, `CartResponse`, `CartItemResponse` (DTOs).
- [ ] `ProductInfo` (`Clients/`): datos del producto que devuelve Products.API.
- [ ] `ErrorCodes` (`CRT_001` … `CRT_005`) y excepciones.

Interfaces → implementaciones:
- [ ] `ICartRepository` → `InMemoryCartRepository`.
- [ ] `IProductsClient` → `ProductsClient`: typed client con `IHttpClientFactory`, que revisa el status antes de leer el body.
- [ ] `ICartService` → `CartService`. El carrito se crea con el primer `POST /items`.

Capa HTTP y transversales (replicando la plantilla):
- [ ] `CartController`, `ErrorResponseWriter` y los cuatro `IExceptionHandler`.
- [ ] `ICorrelationIdAccessor` → `CorrelationIdAccessor`, middlewares, Serilog, Swagger y Health Checks.
- [ ] `Cart.API.http` con requests de éxito y de error.

### Etapa 7 — Orders.API

**Responsable:** Juan Pablo · **Bloque:** 5

Objetivo: creación de órdenes validando contra otros servicios y ciclo de estados.

Tests primero:
- [ ] Crear una orden sin items o con datos inválidos lanza ORD-002.
- [ ] Usuario inexistente lanza ORD-003; producto inexistente, ORD-004; stock insuficiente, ORD-005.
- [ ] `precioUnitario` se toma del producto y `total` se calcula correctamente.
- [ ] `OrderStatusTransitions`: tests parametrizados con todas las transiciones válidas e inválidas (ORD-006).
- [ ] Un estado desconocido en `PUT /status` lanza ORD-002.
- [ ] Listar filtra por `usuarioId` y por `productoId` (D-07).
- [ ] Contrato de errores completo (ORD-001 a ORD-007) en tests de integración.

Lógica de negocio:

Clases concretas:
- [ ] `Order`, `OrderItem`, `OrderStatus` (Models) y `CreateOrderRequest`, `CreateOrderItemRequest`, `UpdateOrderStatusRequest`, `OrderResponse`, `OrderItemResponse`, `OrderStatusResponse` (DTOs).
- [ ] `UserInfo` y `ProductInfo` (`Clients/`).
- [ ] `ErrorCodes` (`ORD_001` … `ORD_007`) y excepciones.
- [ ] `OrderStatusTransitions`: máquina de estados. Pendiente → Confirmada → Enviada → Entregada; Pendiente o Confirmada → Cancelada.

Interfaces → implementaciones:
- [ ] `IOrderRepository` → `InMemoryOrderRepository`.
- [ ] `IUsersClient` → `UsersClient` (se puede tomar como referencia el de Notifications).
- [ ] `IProductsClient` → `ProductsClient` (se puede tomar como referencia el de Cart).
- [ ] `IOrderService` → `OrderService`.

Capa HTTP y transversales (replicando la plantilla):
- [ ] `OrdersController`, `ErrorResponseWriter` y los cuatro `IExceptionHandler`.
- [ ] `ICorrelationIdAccessor` → `CorrelationIdAccessor`, middlewares, Serilog, Swagger y Health Checks.
- [ ] `Orders.API.http` con requests de éxito y de error.

### Etapa 8 — Notifications.API

**Responsable:** Juan Pablo · **Bloques:** 3 (lógica) y 4 (capa HTTP y transversales)

Objetivo: servicio de soporte para notificaciones.

Tests primero:
- [ ] Usuario inexistente lanza NTF-001.
- [ ] Datos faltantes o `tipo` distinto de Email, Push o SMS lanza NTF-002.
- [ ] Enviar registra la notificación con el estado que informa `INotificationSender` (`Enviada`) y con `FechaEnvio`.
- [ ] Listar un usuario sin notificaciones lanza NTF-003.
- [ ] Contrato de errores completo (NTF-001 a NTF-004) en tests de integración.

Lógica de negocio (Bloque 3):

Clases concretas:
- [ ] `Notification`, `NotificationType`, `NotificationStatus` (Models) y `SendNotificationRequest`, `NotificationResponse` (DTOs).
- [ ] `UserInfo` (`Clients/`).
- [ ] `ErrorCodes` (`NTF_001` … `NTF_004`) y excepciones.

Interfaces → implementaciones:
- [ ] `INotificationRepository` → `InMemoryNotificationRepository`.
- [ ] `IUsersClient` → `UsersClient`, contra `GET /api/users/{id}` (contrato de la sección 4.4).
- [ ] `INotificationSender` → `SimulatedNotificationSender`.
- [ ] `INotificationService` → `NotificationService`.

Capa HTTP y transversales (Bloque 4, replicando la plantilla):
- [ ] `NotificationsController`, `ErrorResponseWriter` y los cuatro `IExceptionHandler`.
- [ ] `ICorrelationIdAccessor` → `CorrelationIdAccessor`, middlewares, Serilog, Swagger y Health Checks.
- [ ] `Notifications.API.http` con requests de éxito y de error.

### Etapa 9 — Integración entre servicios

**Responsables:** Thomas (Bloque 5) y Juan Pablo (Bloque 6)

Objetivo: que los servicios colaboren de punta a punta.

Tests primero:
- [ ] `CorrelationIdDelegatingHandler`: las llamadas salientes incluyen el `X-Correlation-Id` del request original.
- [ ] `DownstreamServiceHealthCheck`: `/health/ready` informa `Degraded` si un servicio del que depende no responde.

Thomas (Products y Cart):
- [ ] `IOrdersClient` → `OrdersClient` (HTTP, con `OrderInfo`), probado contra el contrato de la sección 4.4. Se elimina `StubOrdersClient`.
- [ ] `CorrelationIdDelegatingHandler` (hereda de `DelegatingHandler`) en los clientes HTTP de Products y Cart.
- [ ] `DownstreamServiceHealthCheck` (implementa `IHealthCheck`) en `/health/ready` de Products (depende de Orders) y Cart (depende de Products).
- [ ] Timeouts configurados en los clientes HTTP.

Juan Pablo (Orders y Notifications):
- [ ] `CorrelationIdDelegatingHandler` en los clientes HTTP de Orders y Notifications.
- [ ] `DownstreamServiceHealthCheck` en `/health/ready` de Orders (depende de Users y Products) y Notifications (depende de Users).
- [ ] Timeouts configurados en los clientes HTTP.
- [ ] (Opcional) Orders.API notifica a Notifications.API cuando cambia el estado de una orden.
- [ ] (Opcional) Descontar stock al crear una orden (D-14).

### Etapa 10 — Documentación y entregables

**Responsables:** Thomas y Juan Pablo · **Bloque:** 6

Thomas:
- [ ] README: cómo ejecutar cada servicio, tabla de puertos y descripción de cada servicio.
- [ ] Diagrama de arquitectura (ASCII o imagen) en el README.
- [ ] Script para levantar los cinco servicios juntos.
- [ ] Capturas de Swagger UI con ejemplos de error (`errorCode` y `errorMessage`) en `docs/`.

Juan Pablo:
- [ ] Tabla completa de códigos de error en el README, incluyendo USR-007.
- [ ] Decisiones de diseño (sección 5 de este documento) en el README.

Ambos:
- [ ] Archivos `.http` de sus servicios con casos de éxito y de error, revisados para la demo.

### Etapa 11 — Prueba integral y defensa

**Responsables:** Thomas y Juan Pablo (juntos) · **Bloque:** 7

- [ ] Recorrer de punta a punta todo el catálogo de errores de los cinco servicios.
- [ ] Verificar logs (consola y archivo) y health checks de cada servicio.
- [ ] Guion de la demo (15 a 20 minutos): endpoints exitosos y de error desde Swagger, logs y health checks.
- [ ] Revisión cruzada: cada integrante explica en voz alta un servicio del otro.
- [ ] PR final `develop` → `main` (Thomas).

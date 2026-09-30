# Arquitectura — E-Commerce con microservicios

Diagramas de apoyo para el [plan de desarrollo](plan-de-desarrollo.md). GitHub los dibuja automáticamente (Mermaid); en VS Code o Rider se ven con una extensión de Mermaid.

---

## 1. Vista general: servicios y comunicación

Cada caja es un microservicio independiente, con su propio puerto y su propia persistencia. Las flechas son llamadas HTTP (`IHttpClientFactory`) y todas llevan el header `X-Correlation-Id`.

```mermaid
flowchart LR
    cliente(["Cliente<br/>(Swagger / .http)"])

    subgraph ecommerce["ECommerce.slnx"]
        products["<b>Products.API</b><br/>:5001<br/>Thomas"]
        users["<b>Users.API</b><br/>:5002<br/>Juan Pablo"]
        orders["<b>Orders.API</b><br/>:5003<br/>Juan Pablo"]
        cart["<b>Cart.API</b><br/>:5004<br/>Thomas"]
        notifications["<b>Notifications.API</b><br/>:5005<br/>Juan Pablo"]
    end

    cliente --> products & users & orders & cart & notifications

    cart -- "GET /api/products/{id}<br/>existencia y stock" --> products
    orders -- "GET /api/products/{id}<br/>precio y stock" --> products
    orders -- "GET /api/users/{id}<br/>ORD-003" --> users
    notifications -- "GET /api/users/{id}<br/>NTF-001" --> users
    products -. "GET /api/orders?productoId=<br/>PRD-004 (Etapa 9)" .-> orders
```

La flecha punteada es la única dependencia en sentido inverso (Products ↔ Orders). Hasta la Etapa 9, Products usa `StubOrdersClient` y no llama a Orders.

---

## 2. Capas dentro de cada API

Todas las APIs tienen las mismas capas. Cada capa depende solo de **interfaces** de la capa siguiente; `ServiceCollectionExtensions` es el único que conoce las implementaciones.

```mermaid
flowchart TB
    http(["Request HTTP"])
    mw["Middlewares<br/>CorrelationIdMiddleware · RequestLoggingMiddleware"]
    ctrl["Controllers<br/>solo traducen HTTP ↔ DTO"]
    svc["Services<br/>reglas de negocio · lanzan excepciones de dominio"]
    repo["Repositories<br/>persistencia (en memoria → librería de la cátedra)"]
    cli["Clients<br/>llamadas a otros microservicios"]
    eh["ExceptionHandlers<br/>excepción → JSON de error del contrato"]

    http --> mw --> ctrl --> svc
    svc --> repo
    svc --> cli
    svc -. "throw NotFoundException /<br/>BusinessRuleException /<br/>ValidationException" .-> eh
    eh -. "4xx / 5xx con errorCode,<br/>errorMessage y correlationId" .-> http
```

---

## 3. Diagrama de clases — Products.API (plantilla)

Products.API es la plantilla que se replica en el resto de los servicios. Las clases marcadas `«interface»` son las abstracciones; las flechas punteadas con triángulo (`..|>`) indican "implementa" y las flechas punteadas simples (`..>`) indican "depende de / usa".

```mermaid
classDiagram
    direction TB

    class ProductsController {
        -IProductService productService
        +GetAll(categoria, nombre) 200
        +GetById(id) 200 / 404
        +Create(request) 201 / 400 / 409
        +Update(id, request) 200 / 400 / 404
        +Delete(id) 204 / 404 / 409
    }

    class IProductService {
        <<interface>>
        +GetAllAsync(categoria, nombre, ct) IReadOnlyList~ProductResponse~
        +GetByIdAsync(id, ct) ProductResponse
        +CreateAsync(CreateProductRequest, ct) ProductResponse
        +UpdateAsync(id, UpdateProductRequest, ct) ProductResponse
        +DeleteAsync(id, ct)
    }

    class ProductService {
        -IProductRepository repository
        -IOrdersClient ordersClient
        -TimeProvider timeProvider
        -GetExistingProductAsync(id, ct) Product
        -EnsureNameIsUniqueInCategoryAsync(nombre, categoria, ct)
    }

    class IProductRepository {
        <<interface>>
        +GetAllAsync(ct) IReadOnlyList~Product~
        +GetByIdAsync(id, ct) Product?
        +AddAsync(Product, ct)
        +UpdateAsync(Product, ct)
        +DeleteAsync(id, ct)
    }

    class InMemoryProductRepository {
        -ConcurrentDictionary~Guid, Product~ products
    }

    class ProductSeedData {
        <<static>>
        +Create() IReadOnlyList~Product~
    }

    class IOrdersClient {
        <<interface>>
        +HasActiveOrdersAsync(productId, ct) bool
    }

    class StubOrdersClient {
        siempre false (hasta la Etapa 9)
    }

    class OrdersClient {
        HTTP a Orders.API (Etapa 9)
    }

    class TimeProvider {
        <<abstract, .NET>>
        +GetUtcNow() DateTimeOffset
    }

    class Product {
        +Guid Id
        +string Nombre
        +string? Descripcion
        +decimal Precio
        +int Stock
        +string Categoria
        +DateTime FechaCreacion
    }

    class CreateProductRequest {
        <<record>>
        +string Nombre
        +string? Descripcion
        +decimal? Precio
        +int? Stock
        +string Categoria
    }

    class UpdateProductRequest {
        <<record>>
        mismos campos que CreateProductRequest
    }

    class ProductResponse {
        <<record>>
        +Guid Id
        +string Nombre
        +string? Descripcion
        +decimal Precio
        +int Stock
        +string Categoria
        +DateTime FechaCreacion
        +FromEntity(Product)$ ProductResponse
    }

    ProductsController ..> IProductService
    ProductService ..|> IProductService
    ProductService ..> IProductRepository
    ProductService ..> IOrdersClient
    ProductService ..> TimeProvider
    InMemoryProductRepository ..|> IProductRepository
    InMemoryProductRepository ..> ProductSeedData : datos iniciales
    StubOrdersClient ..|> IOrdersClient
    OrdersClient ..|> IOrdersClient

    IProductService ..> CreateProductRequest : recibe
    IProductService ..> UpdateProductRequest : recibe
    IProductService ..> ProductResponse : devuelve
    ProductResponse ..> Product : FromEntity
    IProductRepository ..> Product : persiste
```

### Excepciones y manejo de errores

Cada servicio lanza excepciones de dominio con un `ErrorCode` del catálogo; un `IExceptionHandler` por tipo las convierte en la respuesta de error del contrato. Se registran del más específico al más genérico: el `GlobalExceptionHandler` atrapa todo lo no previsto y devuelve PRD-005.

```mermaid
classDiagram
    direction TB

    class Exception {
        <<.NET>>
        +string Message
    }

    class NotFoundException {
        +string ErrorCode
    }
    class BusinessRuleException {
        +string ErrorCode
        +int StatusCode
    }
    class ValidationException {
        +string ErrorCode
    }

    class ErrorCodes {
        <<static>>
        +PRD_001 = "PRD-001"
        +PRD_002 = "PRD-002"
        +PRD_003 = "PRD-003"
        +PRD_004 = "PRD-004"
        +PRD_005 = "PRD-005"
    }

    class IExceptionHandler {
        <<interface, .NET>>
        +TryHandleAsync(context, exception, ct) bool
    }

    class NotFoundExceptionHandler { 404 }
    class BusinessRuleExceptionHandler { 401 / 403 / 409 / 422 }
    class ValidationExceptionHandler { 400 }
    class GlobalExceptionHandler { 500 · PRD-005 }
    class ErrorResponseWriter {
        +WriteAsync(context, status, errorCode, errorMessage)
    }

    Exception <|-- NotFoundException
    Exception <|-- BusinessRuleException
    Exception <|-- ValidationException

    NotFoundExceptionHandler ..|> IExceptionHandler
    BusinessRuleExceptionHandler ..|> IExceptionHandler
    ValidationExceptionHandler ..|> IExceptionHandler
    GlobalExceptionHandler ..|> IExceptionHandler

    NotFoundExceptionHandler ..> NotFoundException : maneja
    BusinessRuleExceptionHandler ..> BusinessRuleException : maneja
    ValidationExceptionHandler ..> ValidationException : maneja

    NotFoundExceptionHandler ..> ErrorResponseWriter
    BusinessRuleExceptionHandler ..> ErrorResponseWriter
    ValidationExceptionHandler ..> ErrorResponseWriter
    GlobalExceptionHandler ..> ErrorResponseWriter
```

> Los handlers y `ErrorResponseWriter` se implementan en la Etapa 2.

---

## 4. Recorrido de un request con error

Ejemplo: `DELETE /api/products/{id}` sobre un producto que tiene órdenes activas (PRD-004).

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant Ctrl as ProductsController
    participant Svc as ProductService
    participant Repo as IProductRepository
    participant Ord as IOrdersClient
    participant EH as BusinessRuleExceptionHandler

    C->>Ctrl: DELETE /api/products/{id}
    Ctrl->>Svc: DeleteAsync(id)
    Svc->>Repo: GetByIdAsync(id)
    Repo-->>Svc: Product
    Svc->>Ord: HasActiveOrdersAsync(id)
    Ord-->>Svc: true
    Svc--)EH: throw BusinessRuleException(PRD-004, 409)
    Note over Ctrl,EH: El controller no tiene try/catch:<br/>la excepción sube hasta UseExceptionHandler()
    EH-->>C: 409 { errorCode: "PRD-004", errorMessage, correlationId, ... }
```

Si el producto no existiera, el paso 4 devolvería `null`, el servicio lanzaría `NotFoundException(PRD-001)` y **nunca** se consultaría a Orders.

---

## 5. Cómo se replica en el resto de los servicios

| Pieza | Products | Users | Cart | Orders | Notifications |
|---|---|---|---|---|---|
| Service | `IProductService` → `ProductService` | `IUserService` → `UserService` | `ICartService` → `CartService` | `IOrderService` → `OrderService` | `INotificationService` → `NotificationService` |
| Repository | `IProductRepository` → `InMemoryProductRepository` | `IUserRepository` → `InMemoryUserRepository` | `ICartRepository` → `InMemoryCartRepository` | `IOrderRepository` → `InMemoryOrderRepository` | `INotificationRepository` → `InMemoryNotificationRepository` |
| Clients | `IOrdersClient` | — | `IProductsClient` | `IUsersClient`, `IProductsClient` | `IUsersClient` |
| Regla propia | — | `AccountLockoutPolicy` | — | `OrderStatusTransitions` | `INotificationSender` → `SimulatedNotificationSender` |
| Códigos | PRD-001…005 | USR-001…007 | CRT-001…005 | ORD-001…007 | NTF-001…004 |

El detalle completo (carpetas, ciclos de vida y etapa de cada pieza) está en la sección 2.3 del plan.

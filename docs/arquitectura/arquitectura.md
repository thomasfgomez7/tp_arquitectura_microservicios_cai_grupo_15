# Arquitectura — E-Commerce con microservicios

Diagramas de apoyo para el [plan de desarrollo](../planificacion/plan-de-desarrollo.md). GitHub los dibuja automáticamente (Mermaid); en VS Code o Rider se ven con una extensión de Mermaid.

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
    products -. "GET /api/orders?productoId=<br/>PRD-004" .-> orders
```

La flecha punteada es la única dependencia en sentido inverso (Products ↔ Orders): Products solo llama a Orders al borrar un producto. Si Orders no responde, el borrado falla con PRD-005 y no se hace (D-39).

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

    class OrdersClient {
        GET /api/orders?productoId=
    }

    class OrderInfo {
        <<record>>
        +Guid Id
        +string Estado
        +EstaActiva bool
    }

    class CorrelationIdDelegatingHandler {
        agrega X-Correlation-Id
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
    OrdersClient ..|> IOrdersClient
    OrdersClient ..> OrderInfo : lee
    OrdersClient ..> CorrelationIdDelegatingHandler : HttpClient con

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

## 5. Cart.API: un servicio que consume a otro

Cart.API repite la plantilla de Products y suma la primera **comunicación entre servicios**: para agregar un producto, consulta su existencia y su stock en Products.API.

### 5.1 Diagrama de clases

`CartService` depende de `IProductsClient`, no de HTTP. `ProductsClient` recibe un `HttpClient` ya configurado por `IHttpClientFactory` con la URL de `appsettings.json`. En los tests de integración, `FakeProductsClient` reemplaza al cliente real.

```mermaid
classDiagram
    direction TB

    class CartController {
        -ICartService cartService
        +Get(userId) 200 / 404
        +AddItem(userId, request) 200 / 400 / 404 / 422
        +UpdateItem(userId, productId, request) 200 / 400 / 404 / 422
        +RemoveItem(userId, productId) 204 / 404
        +Clear(userId) 204 / 404
    }

    class ICartService {
        <<interface>>
        +GetAsync(usuarioId, ct) CartResponse
        +AddItemAsync(usuarioId, AddCartItemRequest, ct) CartResponse
        +UpdateItemAsync(usuarioId, productoId, UpdateCartItemRequest, ct) CartResponse
        +RemoveItemAsync(usuarioId, productoId, ct)
        +ClearAsync(usuarioId, ct)
    }

    class CartService {
        -ICartRepository repository
        -IProductsClient productsClient
        -TimeProvider timeProvider
        -EnsureStockAsync(productoId, cantidad, ct)
    }

    class ICartRepository {
        <<interface>>
        +GetByUserIdAsync(usuarioId, ct) ShoppingCart?
        +SaveAsync(ShoppingCart, ct)
        +DeleteAsync(usuarioId, ct)
    }

    class InMemoryCartRepository {
        -ConcurrentDictionary~Guid, ShoppingCart~ carts
    }

    class IProductsClient {
        <<interface>>
        +GetProductAsync(productId, ct) ProductInfo?
    }

    class ProductsClient {
        -HttpClient httpClient
        GET api/products/id
    }

    class FakeProductsClient {
        catálogo fijo (solo en tests)
    }

    class IHttpClientFactory {
        <<interface, .NET>>
    }

    class ShoppingCart {
        +Guid UsuarioId
        +List~CartItem~ Items
        +DateTime FechaActualizacion
        +FindItem(productoId) CartItem?
        +QuantityAfterAdding(productoId, cantidad) int
        +AddItem(productoId, cantidad)
        +RemoveItem(productoId)
    }

    class CartItem {
        +Guid ProductoId
        +int Cantidad
    }

    class ProductInfo {
        <<record>>
        +Guid Id
        +string Nombre
        +decimal Precio
        +int Stock
    }

    CartController ..> ICartService
    CartService ..|> ICartService
    CartService ..> ICartRepository
    CartService ..> IProductsClient
    InMemoryCartRepository ..|> ICartRepository
    ProductsClient ..|> IProductsClient
    FakeProductsClient ..|> IProductsClient
    IHttpClientFactory ..> ProductsClient : crea con BaseUrl
    ICartRepository ..> ShoppingCart : persiste
    ShoppingCart *-- CartItem
    IProductsClient ..> ProductInfo : devuelve
```

### 5.2 Agregar un producto al carrito

`POST /api/cart/{userId}/items` con stock insuficiente (CRT-003). Las líneas punteadas a la derecha son la llamada HTTP real entre los dos servicios.

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant Ctrl as CartController
    participant Svc as CartService
    participant Repo as ICartRepository
    participant PC as ProductsClient
    participant P as Products.API

    C->>Ctrl: POST /api/cart/{userId}/items<br/>{ productoId, cantidad: 5 }
    Ctrl->>Svc: AddItemAsync(userId, request)
    Svc->>Repo: GetByUserIdAsync(userId)
    Repo-->>Svc: carrito (o null: se crea uno nuevo)
    Note over Svc: cantidad total = la que ya había + 5 (D-13)
    Svc->>PC: GetProductAsync(productoId)
    PC->>P: GET /api/products/{productoId}
    P-->>PC: 200 { stock: 2, ... }
    PC-->>Svc: ProductInfo
    Svc--)Ctrl: throw BusinessRuleException(CRT-003, 422)
    Note over Ctrl: sin try/catch, la excepción sigue hasta UseExceptionHandler
    Ctrl-->>C: 422 { errorCode: "CRT-003", errorMessage: "Stock insuficiente. Disponible: 2, solicitado: 5." }
```

Cómo reacciona según lo que responde Products.API:

| Products.API responde | `ProductsClient` devuelve | Cart responde |
|---|---|---|
| 200 con stock suficiente | `ProductInfo` | 200 con el carrito actualizado |
| 200 con stock insuficiente | `ProductInfo` | 422, CRT-003 |
| 404 | `null` | 404, CRT-002 |
| 500, 503 o no responde | lanza una excepción | 500, CRT-005 (D-28) |

---

## 6. Cómo se replica en el resto de los servicios

| Pieza | Products | Users | Cart | Orders | Notifications |
|---|---|---|---|---|---|
| Service | `IProductService` → `ProductService` | `IUserService` → `UserService` | `ICartService` → `CartService` | `IOrderService` → `OrderService` | `INotificationService` → `NotificationService` |
| Repository | `IProductRepository` → `InMemoryProductRepository` | `IUserRepository` → `InMemoryUserRepository` | `ICartRepository` → `InMemoryCartRepository` | `IOrderRepository` → `InMemoryOrderRepository` | `INotificationRepository` → `InMemoryNotificationRepository` |
| Clients | `IOrdersClient` | — | `IProductsClient` | `IUsersClient`, `IProductsClient` | `IUsersClient` |
| Regla propia | — | `AccountLockoutPolicy` | `ShoppingCart` (operaciones sobre items) | `OrderStatusTransitions` | `INotificationSender` → `SimulatedNotificationSender` |
| Códigos | PRD-001…005 | USR-001…007 | CRT-001…005 | ORD-001…007 | NTF-001…004 |
| Estado (01/10) | ✅ Completo | 🟡 En curso | ✅ Completo | ⬜ Pendiente | 🟡 En curso |

El detalle completo (carpetas, ciclos de vida y etapa de cada pieza) está en la sección 2.3 del plan.

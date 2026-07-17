# Clean Architecture — Modelo Onion para proyectos .NET
### Documento de referencia genérico

> **Propósito de este documento**: definir, de forma independiente de cualquier producto específico, cómo debe estructurarse un proyecto .NET que implemente Clean Architecture bajo el modelo Onion (Cebolla) — qué va en cada capa, por qué, cómo se relacionan las capas entre sí, y cómo auditar un proyecto existente contra este estándar. Este documento es la base de comparación para cualquier proyecto nuevo o existente, no un análisis de un caso particular.

---

## 1. El patrón en una imagen

```
                    ┌─────────────────────────────────────────┐
                    │   Presentation / Api (Composition Root)  │
                    │  Controllers · Contracts · Middleware    │
                    │  ┌───────────────────────────────────┐  │
                    │  │        Infrastructure              │  │
                    │  │  EF Core · Clientes HTTP · Auth     │  │
                    │  │  ┌───────────────────────────────┐ │  │
                    │  │  │       Application              │ │  │
                    │  │  │  Casos de uso · Interfaces      │ │  │
                    │  │  │  ┌───────────────────────────┐ │ │  │
                    │  │  │  │   Domain / Core           │ │ │  │
                    │  │  │  │  Entities · Enums ·        │ │ │  │
                    │  │  │  │  Excepciones · Constantes  │ │ │  │
                    │  │  │  └───────────────────────────┘ │ │  │
                    │  │  └───────────────────────────────┘ │  │
                    │  └───────────────────────────────────┘  │
                    └─────────────────────────────────────────┘
```

Cuatro anillos concéntricos. **La única regla que no admite excepción**: las flechas de dependencia siempre apuntan hacia adentro.

| Anillo | Puede depender de | Nunca puede depender de |
|---|---|---|
| Domain / Core | *nada* | Application, Infrastructure, Api |
| Application | Domain | Infrastructure, Api |
| Infrastructure | Domain, Application | Api |
| Api (Presentation) | Application, Infrastructure (solo para *wiring* de DI) | — (es el anillo externo) |

Esto se llama la **Regla de Dependencia** (*Dependency Rule*, Robert C. Martin) y es el único invariante verdaderamente no negociable del patrón. Todo lo demás en este documento —nombres de carpeta, convenciones— es una forma recomendada de respetar esa regla, no la regla misma.

---

## 2. Principios que sostienen las 4 capas

Estos principios se repiten en todas las capas de este documento porque son la razón detrás de cada regla concreta. Cuando una convención específica no esté clara, volver a estos principios resuelve la duda.

### 2.1 Regla de Dependencia (Dependency Rule)
El código fuente de una capa interna nunca debe mencionar, importar, ni referenciar por nombre nada de una capa externa. Se verifica de dos formas, en este orden de confiabilidad:
1. **Grafo de `ProjectReference`** en los `.csproj` — la prueba definitiva. Si `Domain.csproj` no tiene ninguna `ProjectReference`, es estructuralmente imposible que dependa de nada.
2. **Búsqueda de `using`/namespaces cruzados** en el código — detecta violaciones que el grafo de proyectos permite pero que no deberían ocurrir en la práctica (ej. Api referenciando Infrastructure para algo más que el *wiring* de arranque).

### 2.2 Inversión de Dependencias (Dependency Inversion Principle)
Las capas internas definen **interfaces** (puertos) para todo lo que necesitan de una capa externa; las capas externas las **implementan** (adaptadores). El flujo de *control* en tiempo de ejecución puede ir de adentro hacia afuera (Application llama a un repositorio), pero el flujo de *dependencia de código* siempre es hacia adentro (Application solo conoce la interfaz, no la clase concreta).

### 2.3 Ignorancia de persistencia (Persistence Ignorance)
Las entidades de dominio no saben cómo se guardan. Cero atributos de ORM (`[Table]`, `[Column]`, `[Key]`), cero herencia de clases base de un framework de persistencia, cero referencia a `DbContext`. El mapeo objeto-relacional se configura *desde afuera* (Fluent API en Infrastructure), nunca *desde dentro* de la entidad.

### 2.4 Ignorancia de presentación (Presentation Ignorance)
El dominio y los casos de uso no saben que existe HTTP. Nada de `IActionResult`, códigos de estado HTTP, atributos de `[FromBody]`/`[FromQuery]`, ni tipos de ASP.NET en Domain o Application. Los casos de uso devuelven resultados con vocabulario de negocio (un enum de estado, un DTO); la capa Api traduce eso a la respuesta HTTP.

### 2.5 Composition Root único
Existe un único punto en toda la solución donde se conocen y conectan las 4 capas: el arranque de la aplicación (`Program.cs`/`Startup.cs`). Cada capa expone un método de extensión de registro propio (`AddApplication()`, `AddInfrastructure()`) que el Composition Root invoca; ninguna otra clase debería llamar `services.AddScoped(...)` fuera de esos métodos de registro.

### 2.6 Separación por *razón de cambio*
Cada capa cambia por una razón distinta: el dominio cambia cuando cambia el negocio; Application cuando cambia un caso de uso; Infrastructure cuando cambia una tecnología (de SQL Server a PostgreSQL, de Azure Blob a S3); Api cuando cambia el canal de exposición (REST a GraphQL, agregar gRPC). Si un cambio de tecnología obliga a tocar Domain, la capa está mal ubicada.

---

## 3. Capa 1 — Domain / Core

### 3.1 Qué va aquí

| Carpeta | Contenido | Ejemplo |
|---|---|---|
| `Entities/` | Clases que representan los conceptos centrales del negocio, una por archivo | `Order`, `Customer`, `Invoice` |
| `Common/` (o `SeedWork/`) | Tipos base compartidos por las entidades: `BaseEntity`, `AuditableEntity`, marcador `IEntity` (ver §3.2) | — |
| `Enums/` | Un enum por archivo — vocabulario cerrado del dominio (ver §3.5) | `OrderStatus`, `PaymentMethod` |
| `Exceptions/` | Jerarquía de excepciones de dominio, con una base abstracta (ver §3.3) | `DomainException` → `NotFoundException`, `BusinessRuleException` |
| `Constants/` | Valores fijos que son parte del lenguaje del negocio, nunca configuración (ver §3.4) | `RoleNames`, `SdlcPhases` |
| `ValueObjects/` (si aplica) | Tipos inmutables definidos por su valor, no por identidad | `Money`, `Address`, `Email` |

### 3.2 `BaseEntity` vs. `AuditableEntity`: qué son y cuándo usar cada uno

**Qué son.** Son los dos tipos base de los que hereda toda entidad, definidos una sola vez en `Common/` (nunca se redeclaran por entidad):

- **`BaseEntity`** — el contrato mínimo: solo un identificador (`Id`). No sabe nada de cuándo se creó o modificó el registro.
- **`AuditableEntity`** — hereda de `BaseEntity` y añade el rastro temporal: `CreatedDate` y `UpdatedDate`. Estos dos campos **no se asignan a mano** en cada servicio; los estampa automáticamente un interceptor de `SaveChanges` en Infrastructure (ver §5.6), así que ninguna entidad ni servicio necesita conocer la hora actual del sistema.

**Cuándo usar cada uno.** La pregunta correcta no es "¿es importante esta tabla?" — es **"¿le importa a alguien CUÁNDO cambió esta fila?"**

| Usar `BaseEntity` cuando… | Usar `AuditableEntity` cuando… |
|---|---|
| La entidad es un **catálogo o definición** — datos de referencia que casi no cambian y cuyo historial temporal no le importa a nadie | La entidad es **transaccional** — algo que un usuario crea, modifica, o cuyo historial de cambios importa para el negocio o para auditoría |
| Ejemplos: `Role`, `Country`, `Currency`, `DocumentType`, un catálogo de precios | Ejemplos: `Order`, `Invoice`, `Payment`, `Comment` — cualquier entidad que un usuario final crea o edita |
| Pregunta guía: "¿a alguien le importa cuándo se creó el rol 'Administrador'?" → normalmente no | Pregunta guía: "¿necesito saber cuándo se hizo este pedido o cuándo se modificó por última vez?" → normalmente sí |

Un error común es heredar de `AuditableEntity` **por defecto**, incluso en catálogos — casi siempre porque se copió la firma de otra entidad sin revisar si de verdad aplica. El síntoma delator: una entidad que **se autodescribe como catálogo en su propio comentario** pero hereda de `AuditableEntity` de todas formas. La regla de decisión nunca es "qué heredan las demás entidades del proyecto" — es la pregunta de la tabla anterior, evaluada entidad por entidad.

**Un tercer caso: un hecho inmutable con un único timestamp.** Ni `BaseEntity` ni `AuditableEntity` — hay entidades que registran algo que ocurrió una sola vez y nunca se edita después (una entrada de auditoría, un evento de dominio, una línea de log): solo necesitan saber *cuándo pasó*, no *cuándo se modificó por última vez*, porque nunca se modifican.

Para estos casos, **no declarar un campo `CreatedDate` suelto, sin su par `UpdatedDate`**. Cualquiera familiarizado con la convención `AuditableEntity` (Created+Updated) asumirá, al ver solo `CreatedDate`, que `UpdatedDate` falta por un olvido — no que la ausencia fue intencional. Nombrar el campo **`Timestamp`** en vez de `CreatedDate` elimina esa ambigüedad de un vistazo: el nombre distinto señala, sin necesitar comentario, que la entidad no sigue el patrón de auditoría de dos campos, sino que registra un hecho puntual e inmutable.

```csharp
namespace MyApp.Domain.Entities;

/// <summary>Entrada de auditoría inmutable — nunca se edita tras crearse, así que no necesita
/// UpdatedDate. El campo se llama Timestamp, no CreatedDate, para que no se lea como una
/// AuditableEntity con la mitad del patrón faltante.</summary>
public class AuditEntry : BaseEntity
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
```

**Ejemplo — mismo dominio, las dos decisiones lado a lado**:
```csharp
namespace MyApp.Domain.Entities;

/// <summary>Catálogo global de roles. No cambia salvo que el negocio cree un rol nuevo;
/// a nadie le interesa cuándo se creó "Administrador" — no necesita auditoría.</summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Un pedido de un cliente. Transaccional: el negocio necesita saber cuándo se
/// creó y cuándo se modificó por última vez (SLA de entrega, disputas, reportes).</summary>
public class Order : AuditableEntity
{
    public int CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
}
```

### 3.3 Excepciones de dominio: qué debe representar cada una

Una excepción de dominio no es "algo salió mal, genérico" — es el mecanismo para comunicar, desde adentro hacia afuera, que ocurrió un **caso puntual del negocio que rompe el flujo normal**: una regla que no se cumplió, un recurso que no existe, un estado que no permite la operación pedida. En la práctica funciona como un código de error *tipado*: en vez de que quien llama tenga que interpretar un string de mensaje o un número de código, el **tipo** de la excepción ya identifica exactamente qué pasó.

**El principio que hay que respetar: cada tipo concreto de excepción representa UN caso de negocio específico y nombrable — nunca un cajón genérico.** Preguntas para verificarlo sobre cualquier excepción del proyecto:
- ¿Su nombre describe la condición de negocio que rompió el flujo (`InsufficientStockException`, `DuplicateEmailException`), o es un nombre técnico genérico (`ApplicationException`, `CustomException`, `BusinessException`)?
- ¿Se lanza desde el punto exacto del código que conoce la regla, o se reutiliza en varios lugares no relacionados con un mensaje distinto cada vez (señal de que en realidad hacen falta varios tipos)?
- ¿Alguien que solo ve el `catch` —sin leer el mensaje— ya sabe qué pasó?

**Estructura recomendada**: una base abstracta común (`DomainException`) de la que heredan las excepciones concretas. Así una capa externa (el middleware de Api, ver §6.5) captura la base una sola vez y decide el resultado —código HTTP, mensaje— según el tipo concreto, sin que el dominio necesite saber que un código HTTP existe.

```csharp
namespace MyApp.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

/// <summary>El recurso solicitado no existe — se lanza desde la búsqueda por id/clave del caso de
/// uso que la necesite ("no existe la Orden 42"), nunca como un catch-all genérico.</summary>
public sealed class NotFoundException(string message) : DomainException(message);

/// <summary>Caso de negocio puntual: se intentó confirmar un pedido cuyo stock ya no alcanza. El
/// nombre describe exactamente la regla que se rompió — no es un "BadRequestException" genérico
/// que obligue a leer el mensaje para saber qué pasó, y lleva los datos del caso para quien la
/// capture (cantidad pedida vs. disponible), no solo un mensaje de texto.</summary>
public sealed class InsufficientStockException(string productName, int requested, int available)
    : DomainException($"Cannot fulfill {requested} units of '{productName}' — only {available} available.")
{
    public string ProductName { get; } = productName;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}
```

Se lanza exactamente en el punto donde el caso de uso detecta la condición — nunca "por si acaso" desde una capa distinta a la que conoce la regla:
```csharp
if (product.StockQuantity < requestedQuantity)
    throw new InsufficientStockException(product.Name, requestedQuantity, product.StockQuantity);
```

### 3.4 Constantes: cuándo crearlas, cómo agruparlas, y `const` vs. `static readonly`

**Regla base: ningún código, mensaje o valor fijo se escribe directamente en la lógica de negocio.** Ni un número mágico, ni un string suelto repetido en varios lugares. Todo valor fijo que el negocio conoce por su nombre —un rol, una fase, un límite, una clave de configuración— se declara una sola vez como constante, con un nombre que explique qué representa, y se referencia desde ahí.

```csharp
// ❌ Número mágico y string suelto — nadie sabe qué significa "5" ni por qué ese rol exacto
if (failedAttempts >= 5) throw new AccountLockedException();
if (role == "Project Manager") { ... }

// ✅ Con constantes con nombre — la intención queda escrita en el código, no solo en la cabeza de quien lo escribió
if (failedAttempts >= AccountLockoutConstants.MaxFailedAttempts) throw new AccountLockedException();
if (role == RoleNames.ProjectManager) { ... }
```

**`const` vs. `public static readonly` — no son intercambiables por estilo; hay una diferencia real de compilación:**

| | `const` | `public static readonly` |
|---|---|---|
| Cuándo se resuelve el valor | En tiempo de **compilación** — se copia ("inlinea") literalmente en el IL de cada ensamblado que lo consume | En tiempo de **ejecución** — cada consumidor lee el valor real desde el ensamblado que lo declara |
| Riesgo | Si otro ensamblado lo referencia y luego el valor cambia, ese consumidor sigue viendo el valor viejo hasta que se recompila — un bug clásico y difícil de detectar ("constante congelada") | Ninguno — siempre se lee el valor vigente |
| Dónde lo exige el propio lenguaje | `case` de un `switch`, argumentos de atributo, valores por defecto de parámetro — el compilador exige ahí un valor constante de compilación | No aplica en esos contextos — el compilador los rechaza |

**Regla de uso de este documento: declarar `public static readonly` por defecto.** Usar `const` únicamente cuando el lenguaje lo exige —un argumento de atributo, un valor por defecto de parámetro, o la condición de un `case`— nunca por costumbre o "porque así se ve en otros archivos".

```csharp
public static class AccountLockoutConstants
{
    // static readonly por defecto: nada obliga aquí a un valor de compilación.
    public static readonly int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}

public static class HttpHeaderNames
{
    // const es obligatorio aquí: se usa como argumento de atributo, que exige constante de compilación.
    public const string CorrelationId = "X-Correlation-Id";
}
```

**Agrupación: únicamente por lógica de negocio, nunca por coincidencia.** Dos constantes van en la misma clase solo si representan el mismo concepto de negocio o se usan en el mismo servicio/flujo — nunca solo porque ambas "son pequeñas" o "son strings". Si no comparten concepto, van en clases separadas, aunque cada una termine con un único valor.

```csharp
// ✅ agrupadas correctamente — las 3 describen el mismo concepto: nombres canónicos de rol
public static class RoleNames
{
    public static readonly string ProjectManager = "Project Manager";
    public static readonly string ProductOwner = "Product Owner";
    public static readonly string TechnicalLead = "Technical Lead";
}

// ❌ agrupadas por coincidencia — nada conecta un límite de reintentos con una URL externa
public static class MiscConstants
{
    public static readonly int MaxRetries = 3;
    public static readonly string GraphApiBaseUrl = "https://graph.microsoft.com";
}

// ✅ separadas — cada una es su propio concepto, aunque tenga un solo valor
public static class RetryPolicyConstants { public static readonly int MaxRetries = 3; }
public static class GraphApiConstants { public static readonly string BaseUrl = "https://graph.microsoft.com"; }
```

### 3.5 Enumeraciones: vocabulario cerrado, validar por valor — no por literal

**Cuándo usar un enum en vez de constantes de string**: cuando el conjunto de valores posibles es cerrado y conocido de antemano, y representa un estado o tipo discreto del negocio (nunca un conjunto abierto que crece por configuración o dato de catálogo — eso son constantes, o una tabla).

```csharp
public enum OrderStatus { Pending, Confirmed, Shipped, Cancelled }
```

**Validar siempre por el valor del enum — nunca por su representación literal:**
```csharp
// ❌ compara contra el string — frágil: un typo, un cambio de idioma o un rename silencioso rompe la lógica
if (order.Status.ToString() == "Confirmed") { ... }

// ✅ compara contra el valor del enum — el compilador lo verifica; no se puede escribir mal
if (order.Status == OrderStatus.Confirmed) { ... }
```

El literal sí tiene un lugar legítimo: como parte de un **mensaje** para humanos (un log, un error, una respuesta) — nunca como parte de la condición que decide el flujo:
```csharp
// ✅ el literal aparece en el mensaje; la decisión que sí importa sigue siendo por valor
if (order.Status != OrderStatus.Confirmed)
    throw new DomainException($"Order must be Confirmed, but it is {order.Status}.");
```

**Se recomienda un `ToString()` (o método de extensión equivalente) que devuelva una etiqueta legible de negocio**, cuando el nombre técnico del miembro no es lo que un usuario final debería leer:
```csharp
public static class OrderStatusExtensions
{
    public static string ToDisplayString(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Awaiting confirmation",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Shipped => "Shipped",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
```

**Serialización JSON: exponer y recibir el NOMBRE del enum, nunca su valor numérico subyacente.** Un cliente de la API no debería memorizar que `1` significa `Confirmed` — ese acoplamiento es innecesario y frágil (el orden de los valores puede cambiar sin querer). Se configura una sola vez, para toda la aplicación:
```csharp
// Program.cs — aplica a los enums de toda solicitud/respuesta JSON
builder.Services.Configure<JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```
```json
// ✅ el cliente envía y recibe el nombre, no el código — igual en la solicitud que en la respuesta
{ "status": "Confirmed" }

// ❌ sin el converter, System.Text.Json serializa por defecto el número subyacente
{ "status": 1 }
```

### 3.6 Protección de datos sensibles: el atributo `SensitiveData`

No todo dato sensible se filtra por la base de datos — muchos se exponen sin querer por una vía distinta: los logs. Un `logger.LogInformation("Creating user {@user}", user)` que serializa la entidad completa expone el hash de la contraseña, un token, un código OTP, en el archivo de log o en el servicio de logging externo (Application Insights, Datadog, un archivo en disco), aunque la base de datos esté perfectamente protegida.

**Solución: un atributo marcador, `[SensitiveData]`, que la propia entidad/DTO aplica sobre sus campos sensibles**, para que cualquier mecanismo de logging (manual o por decorador AOP, ver §5.3) lo detecte por reflexión y reemplace el valor por una máscara antes de que salga de la aplicación — a un log, una métrica, cualquier destino externo al proceso.

```csharp
namespace MyApp.Domain.Security;

/// <summary>Marca una propiedad, campo, parámetro o valor de retorno cuyo valor nunca debe
/// aparecer en logs ni en ningún destino externo a la aplicación. Es un marcador puro — sin
/// lógica, sin dependencias — para que las entidades puedan aplicarlo directamente sobre sus
/// propios campos sin que Domain dependa de nada de Infrastructure.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class SensitiveDataAttribute : Attribute;
```

**Por qué vive en Domain y no en Infrastructure**, aunque su propósito final sea sobre logging (una preocupación técnica): porque son las propias entidades quienes lo aplican directamente sobre sus campos.
```csharp
namespace MyApp.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;

    [SensitiveData] public string PasswordHash { get; set; } = string.Empty;
    [SensitiveData] public string PasswordSalt { get; set; } = string.Empty;
}
```
Si el atributo viviera en Infrastructure, `User` (Domain) tendría que referenciar Infrastructure solo para marcar sus propios campos — invertiría la Regla de Dependencia. Como el atributo no tiene ninguna dependencia (solo hereda de `System.Attribute`), puede vivir en el anillo más interno sin ensuciarlo, exactamente igual que `BaseEntity`/`AuditableEntity` (§3.2). Quien sí *interpreta* el atributo (lo busca por reflexión y aplica el enmascarado) es Infrastructure — ver §5.3.

### 3.7 Qué NO va aquí, y por qué

| Nunca debe aparecer | Por qué |
|---|---|
| Interfaces de repositorio o de servicios externos (`IRepository`, `IEmailSender`) | Son puertos hacia el *exterior* del negocio — pertenecen a Application (ver §4). Ponerlas en Domain no rompe la Regla de Dependencia por sí solo, pero mezcla "qué es el negocio" con "qué necesita el negocio de afuera", dos preocupaciones distintas |
| Atributos de EF Core, JSON, validación de ASP.NET | Viola la Ignorancia de Persistencia/Presentación — el dominio deja de ser reutilizable fuera de esa tecnología |
| DTOs de transporte | Un DTO existe *para cruzar un límite de proceso/serialización*; el dominio no cruza ningún límite, vive en el centro |
| Cualquier `PackageReference` a un framework (`Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`) | Es la prueba más objetiva de una fuga — si Domain necesita un paquete de infraestructura para compilar, alguna clase no debería estar ahí |
| Lógica de casos de uso (orquestar varios repositorios, llamar servicios externos) | Eso es un caso de uso (Application), no una regla de negocio intrínseca a una entidad |

### 3.8 Especificación del proyecto

- `Domain.csproj` (o `Core.csproj`): **cero** `ProjectReference`, **cero** `PackageReference` fuera de la BCL de .NET.
- Es el único proyecto de la solución que se podría, en teoría, mover a otro lenguaje/plataforma sin reescribir nada más que sintaxis.

### 3.9 Ejemplos

**Tipos base compartidos** — declarados una sola vez en `Common/`, ver §3.2 para cuándo usar cada uno:
```csharp
namespace MyApp.Domain.Common;

public interface IEntity { int Id { get; set; } }

public abstract class BaseEntity : IEntity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
```

Para excepciones de dominio, ver §3.3. Para constantes, ver §3.4. Para enumeraciones, ver §3.5. Para el atributo `SensitiveData`, ver §3.6.

### 3.10 Checklist de auditoría — Domain / Core

| # | Criterio | Cómo verificarlo | Peso |
|---|---|---|---|
| D1 | Cero dependencias externas | `Domain.csproj`: 0 `PackageReference`, 0 `ProjectReference` | 1 |
| D2 | Ignorancia de persistencia y framework | `grep` de atributos de EF/JSON/ASP.NET en `Entities/`: 0 resultados | 1 |
| D3 | Cero dependencia de código hacia capas externas | `grep` de `using <Proyecto>.Application/Infrastructure/Api` en Domain: 0 resultados | 1 |
| D4 | No define interfaces de infraestructura | 0 archivos `I*Repository`, `I*Service` cuyo contrato dependa de un detalle técnico externo | 1 |
| D5 | Enums/Excepciones/Constantes forman un vocabulario cerrado, sin números mágicos ni strings sueltos en la lógica | 0 literales numéricos/string repetidos fuera de una constante con nombre; enums validados por valor, no por `.ToString()` | 1 |

---

## 4. Capa 2 — Application

### 4.1 Qué va aquí

| Carpeta | Contenido | Ejemplo |
|---|---|---|
| `Interfaces/Repositories/` | Contratos de persistencia — genéricos y/o enfocados por agregado | `IRepository<T>`, `IUnitOfWork`, `IOrderRepository` |
| `Interfaces/Services/` | Contratos de todo lo demás externo: servicios de infraestructura y servicios de aplicación propios | `IEmailSender`, `ICurrentUser`, `IOrderService` |
| `Services/` | Implementación de los casos de uso — la orquestación real | `OrderService : IOrderService` |
| `DTOs/` | Objetos que cruzan el límite de esta capa hacia Api o hacia servicios externos | `OrderDto`, `CreateOrderRequestDto` |
| `Configuration/` | Clases de *Options* (`IOptions<T>`) que Application y/o Infrastructure consumen | `EmailOptions`, `JwtOptions` |
| `Validation/` (si no hay contrato de transporte separado) | Reglas de validación de negocio reutilizables | `PasswordPolicy` |
| `DependencyInjection.cs` | Único punto de registro de todo lo de esta capa (`AddApplication()`) | — |

### 4.2 Repositorio genérico vs. repositorio enfocado: cuál usar y cuándo

**Regla de preferencia: usar siempre el repositorio genérico (`IRepository<T>` vía `IUnitOfWork`) para operaciones CRUD simples. Crear un `IXxxRepository` enfocado por agregado es la excepción — solo cuando la operación no puede resolverse con las operaciones genéricas —, nunca el punto de partida por defecto.**

El patrón recomendado para exponer persistencia sin acoplar Application a un ORM:
```csharp
namespace MyApp.Application.Interfaces.Repositories;

public interface IRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    // Fábrica con caché por tipo: los servicios dependen solo de IUnitOfWork,
    // no de un IRepository<T> distinto inyectado por cada agregado.
    IRepository<T> Repository<T>() where T : class, IEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Cuándo el genérico es suficiente** (no crear una interfaz nueva): obtener por id, listar todo, buscar por un predicado simple, agregar/actualizar/eliminar sin reglas adicionales de carga o alcance — la inmensa mayoría de las operaciones CRUD de cualquier agregado.

**Cuándo sí se justifica un repositorio enfocado** — solo ante una necesidad real que el genérico no resuelve:

| Necesidad real | Ejemplo |
|---|---|
| *Eager loading* de navegaciones específicas | Cargar `Order` junto con sus `OrderItems` y el `Customer` en una sola consulta |
| Filtros de alcance que no son parte del contrato genérico | Listar solo pedidos del tenant activo (multi-tenant), o excluir soft-deleted |
| Agregaciones o proyecciones cross-entidad | Total de ventas por mes, uniendo `Order` y `Payment` |
| Aislamiento del *change tracker* para una escritura puntual | Registrar un evento de auditoría sin arrastrar cambios pendientes de otra operación |

Cada repositorio enfocado documenta, en su propio archivo, *por qué* existe fuera del genérico — si nadie puede explicarlo en una frase, probablemente debería volver a ser `IRepository<T>`:
```csharp
namespace MyApp.Application.Interfaces.Repositories;

/// <summary>Repositorio enfocado — existe porque GetActiveWithItemsAsync necesita eager loading
/// de OrderItems + Customer, algo que IRepository&lt;T&gt; no expone.</summary>
public interface IOrderRepository
{
    Task<Order?> GetActiveWithItemsAsync(int orderId, CancellationToken ct = default);
}
```

### 4.3 Qué es (y qué no es) un Servicio de Aplicación — la frontera con Infrastructure

Un **Servicio de Aplicación** (`Interfaces/Services/` + `Services/`) orquesta un caso de uso: aplica reglas de negocio, coordina repositorios, y decide un flujo — usando solo objetos y vocabulario del propio negocio. Un servicio que en cambio habla con **un agente externo al negocio** —una API de terceros, un proveedor de correo, una pasarela de pago, el sistema de archivos, un bus de mensajes— no es un Servicio de Aplicación, aunque su *interfaz* se declare en Application: su implementación real pertenece a Infrastructure (ver §5.2, la misma distinción vista desde el otro lado).

La pregunta que decide dónde va la implementación: **¿esta operación depende de una regla del negocio, o depende de un sistema/proveedor fuera del control de la aplicación?**

| Es un Servicio de Aplicación (`Application/Services/`) | Es un puerto cuya implementación va en Infrastructure |
|---|---|
| Calcula un descuento según las reglas del negocio | Envía el correo con el resultado (`IEmailSender`) |
| Decide si un pedido puede confirmarse según su estado y stock | Cobra el pedido en una pasarela de pago externa (`IPaymentGateway`) |
| Aplica la regla de aprobación de un documento | Sube el documento firmado a un almacenamiento externo (`IFileStorage`) |
| Orquesta el flujo "confirmar pedido → notificar → registrar auditoría" | Cada paso que sale del proceso (correo, pago, archivo) es un puerto distinto, implementado afuera |

```csharp
// ✅ Servicio de Aplicación: regla de negocio pura, no sabe que un correo existe
namespace MyApp.Application.Services;

public class OrderPricingService : IOrderPricingService
{
    public decimal CalculateDiscount(CustomerTier tier, decimal subtotal) => tier switch
    {
        CustomerTier.Gold => subtotal * 0.10m,
        CustomerTier.Silver => subtotal * 0.05m,
        _ => 0m
    };
}
```
```csharp
// La INTERFAZ vive en Application (es un puerto que Application necesita)...
namespace MyApp.Application.Interfaces.Services;
public interface IEmailSender { Task SendAsync(string to, string subject, string body, CancellationToken ct = default); }

// ...pero la IMPLEMENTACIÓN real (habla con SMTP/SendGrid/etc., un agente externo) vive en Infrastructure — ver §5.2.
```

### 4.4 DTOs en la frontera de los servicios: nunca entidades

**Toda operación pública de un Servicio de Aplicación recibe y retorna DTOs — nunca una entidad de Domain.** Una entidad es un objeto interno con estado mutable y, si se usa EF Core, rastreado por el *change tracker*; exponerla fuera del servicio permite que un consumidor la modifique sin pasar por las reglas del caso de uso, y lo acopla a la forma interna del dominio (el mismo razonamiento de §6.2, aplicado un nivel más adentro).

Internamente, el servicio sí transforma DTO↔entidad — a mano, o con una librería de mapeo genérica (Mapster, AutoMapper) cuando el volumen de mapeos repetidos lo justifica (ver el anti-patrón de sobre-ingeniería en §9: no adoptar una librería sin presión real de duplicación).

```csharp
public class OrderService(IUnitOfWork uow, IMapper mapper) : IOrderService
{
    public async Task<OrderDto> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await uow.Repository<Order>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Order {id} not found.");

        // Mapeo manual — aceptable con pocos campos y sin repetirse en otro servicio:
        // return new OrderDto(order.Id, order.CustomerName, order.Status, order.TotalAmount);

        // Mapeo con librería — preferible cuando el mismo par Entidad↔DTO se mapea en varios lugares:
        return mapper.Map<OrderDto>(order);
    }
}
```

**Los perfiles de mapeo viven en esta capa** (`Application/Mappings/`), sin importar si el mapeo termina usándolo un servicio de la propia Application o un controller de Api — es Application quien declara cómo se relacionan sus DTOs con sus entidades, porque es quien conoce ambos lados:
```csharp
namespace MyApp.Application.Mappings;

public class OrderMappingProfile : Profile // AutoMapper; con Mapster sería un IRegister
{
    public OrderMappingProfile() => CreateMap<Order, OrderDto>();
}
```

### 4.5 Buenas prácticas con Mapster: `Adapt` vs. `Map`

Cuando la librería elegida es Mapster (no aplica a AutoMapper, que solo expone `Map`), existen **dos formas de ejecutar el mismo mapeo, y no son intercambiables por preferencia de estilo** — cada una tiene una razón de uso distinta:

| | `Adapt<T>()` — método de extensión estático | `Map<T>()` — vía `IMapper` inyectado |
|---|---|---|
| Cómo se invoca | `entity.Adapt<OrderDto>()`, directo sobre el objeto origen | `mapper.Map<OrderDto>(entity)`, sobre una instancia de `IMapper` recibida por constructor |
| De dónde toma la configuración | `TypeAdapterConfig.GlobalSettings` — estático, compartido por todo el proceso | La instancia de `TypeAdapterConfig` registrada en el contenedor de DI |
| Testabilidad | La dependencia es implícita: no aparece en el constructor, no se puede sustituir en un test unitario | La dependencia es explícita (`IMapper mapper`) y se puede mockear/sustituir en un test |
| Coherencia con este documento | Rompe el principio de §4.3/§4.4: toda dependencia de un servicio se recibe inyectada, nunca se invoca estática desde adentro | Es el mismo patrón que ya se sigue con `IUnitOfWork`, `IEmailSender`, etc. |

**Regla de uso: dentro de un Servicio de Aplicación, usar siempre `Map` a través de un `IMapper` inyectado — nunca `Adapt` estático.** Reservar `Adapt` para contextos que ya son estáticos o de una sola ejecución, donde inyectar un mapper no aporta nada: dentro de la configuración misma de un perfil de mapeo (`IRegister`), en un script de migración de datos, en una utilidad sin estado, o en una capa externa como Api cuando el mapeo es tan trivial y local que no justifica una dependencia explícita.

```csharp
// ✅ Dentro de un servicio de aplicación: IMapper inyectado — mismo patrón que cualquier otro puerto.
public class OrderService(IUnitOfWork uow, IMapper mapper) : IOrderService
{
    public async Task<OrderDto> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await uow.Repository<Order>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Order {id} not found.");

        return mapper.Map<OrderDto>(order); // testable: en un test unitario se sustituye IMapper por un fake
    }
}

// ❌ Adapt estático dentro del mismo servicio: la dependencia queda oculta y no se puede sustituir en un test.
public class OrderServiceWrong(IUnitOfWork uow) : IOrderService
{
    public async Task<OrderDto> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await uow.Repository<Order>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Order {id} not found.");

        return order.Adapt<OrderDto>(); // depende de TypeAdapterConfig.GlobalSettings, invisible desde afuera
    }
}

// ✅ Adapt SÍ es apropiado aquí: dentro de la propia configuración del mapeo, no hay nada que inyectar.
namespace MyApp.Application.Mappings;

public class OrderMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<Order, OrderDto>()
            .Map(dest => dest.CustomerLabel, src => src.Customer.Adapt<CustomerSummaryDto>());
}
```

### 4.6 Validadores: reglas de estructura de datos y reglas de negocio

Application declara los validadores de sus propias entidades/DTOs (`Validation/` o `Validators/`). Cada validador cubre dos tipos de regla, y ninguna sustituye a la otra:

| Tipo de regla | Qué valida | Ejemplo |
|---|---|---|
| **Estructura del dato** (a menudo un reflejo de la restricción de base de datos) | Longitud máxima, tipo, si el campo es requerido o admite nulo, valores por defecto | `Name` no supera 100 caracteres porque la columna es `nvarchar(100)`; `Email` es requerido |
| **Regla de negocio** | Invariantes que no vienen de la base de datos sino del dominio | `Quantity` debe ser mayor que cero (*non-zero*); `DiscountPercent` no puede superar el máximo permitido para el nivel del cliente |

```csharp
namespace MyApp.Application.Validation;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequestDto>
{
    public CreateOrderRequestValidator()
    {
        // Regla de estructura — refleja la restricción de la columna en base de datos.
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);

        // Reglas de negocio — no vienen de ninguna columna, son invariantes del dominio.
        RuleFor(x => x.Quantity).GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0.01m)
            .WithMessage("TotalAmount must be at least 0.01.");
    }
}
```

Esto no contradice §6.2/§6.3 (los validadores de los *contratos de transporte* de Api): son dos fronteras distintas, cada una con su propio validador. El de Api protege la forma que entra por HTTP; el de Application protege la forma que entra a un caso de uso, sin importar si vino de HTTP, de un job programado o de otro servicio interno.

**El validador se ejecuta dentro del servicio — no solo en el borde HTTP.** Un Servicio de Aplicación puede invocarse desde más de un punto de entrada: un controller de Api, un job programado, un comando de consola, otro servicio interno, una prueba automatizada. Si la única validación que existe vive en un filtro de Api (§6.3), cualquier invocación que no pase por HTTP la evita por completo — la regla de negocio queda, en la práctica, sin protección real. Por eso el propio servicio valida su entrada antes de ejecutar la operación, con el mismo validador (inyectado, igual que cualquier otro puerto):

```csharp
namespace MyApp.Application.Services;

public class OrderService(IUnitOfWork uow, IValidator<CreateOrderRequestDto> validator) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequestDto request, CancellationToken ct = default)
    {
        // Se valida aquí, adentro del caso de uso — no solo cuando la llamada viene de un controller.
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new DomainValidationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));

        var order = new Order { CustomerId = request.CustomerId, TotalAmount = request.TotalAmount };
        await uow.Repository<Order>().AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return new OrderDto(order.Id, request.CustomerName, order.Status, order.TotalAmount);
    }
}
```

La excepción resultante (`DomainValidationException`, ver §3.3) es la misma sin importar quién llamó al servicio; quien la traduce a un código HTTP 400 es exclusivamente Api (§6.3) — el servicio ni sabe ni necesita saber que HTTP existe. Los dos puntos de validación —el filtro de Api y el propio servicio— no son redundantes: cada uno protege una puerta de entrada distinta.

### 4.7 Qué NO va aquí, y por qué

| Nunca debe aparecer | Por qué |
|---|---|
| Una implementación concreta de acceso a datos (`DbContext`, SQL) | Application solo conoce la *interfaz* de persistencia; la implementación es un detalle de Infrastructure |
| Tipos de ASP.NET Core (`ControllerBase`, `IActionResult`, `HttpContext`) | Viola la Ignorancia de Presentación — un caso de uso no debería cambiar si el canal HTTP cambia |
| Referencia de código a Infrastructure o a Api | Rompería la Regla de Dependencia; se verifica con `grep` de los namespaces de esas capas |
| Los DTOs de transporte de Api (contratos HTTP) reutilizados como si fueran los DTOs de Application | Cada límite tiene su propio contrato — ver §6.2 |
| La implementación real de un servicio que habla con un agente externo | Eso pertenece a Infrastructure — ver §4.3 y §5.2 |

### 4.8 Especificación del proyecto

- `Application.csproj`: **una sola** `ProjectReference` (a Domain). Paquetes permitidos: únicamente los de la familia `Microsoft.Extensions.*.Abstractions` (Logging, Options, DependencyInjection) — más, si se usan, los paquetes *abstractos* de mapeo/validación (`AutoMapper`, `Mapster`, `FluentValidation`) — nunca la implementación concreta de un framework de infraestructura.

### 4.9 Ejemplos

**Interfaz de servicio externo** (el puerto — ver §4.3 para la distinción con su implementación):
```csharp
namespace MyApp.Application.Interfaces.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
```

**DTO** — sufijo `Dto` consistente, evita colisión de nombre con la entidad homónima:
```csharp
namespace MyApp.Application.DTOs;

public record OrderDto(int Id, string CustomerName, OrderStatus Status, decimal TotalAmount);
```

**Servicio de aplicación** — orquesta puertos, nunca hace I/O directo, recibe/retorna DTOs (ver §4.4):
```csharp
namespace MyApp.Application.Services;

public class OrderService(IUnitOfWork uow, IEmailSender email) : IOrderService
{
    public async Task<OrderDto> ConfirmAsync(int orderId, CancellationToken ct = default)
    {
        var repo = uow.Repository<Order>();
        var order = await repo.GetByIdAsync(orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        order.Status = OrderStatus.Confirmed;
        await uow.SaveChangesAsync(ct);
        await email.SendAsync(order.CustomerEmail, "Order confirmed", "...", ct);

        return new OrderDto(order.Id, order.CustomerName, order.Status, order.TotalAmount);
    }
}
```

### 4.10 Checklist de auditoría — Application

| # | Criterio | Cómo verificarlo | Peso |
|---|---|---|---|
| A1 | Depende solo de Domain | `Application.csproj`: 1 `ProjectReference` (Domain); sin paquetes de implementación concreta | 1 |
| A2 | Define los puertos de toda interacción externa | Toda dependencia externa (persistencia, email, APIs) tiene una interfaz aquí, no una clase concreta | 1 |
| A3 | Cero referencia real de código a Infrastructure o Api | `grep` de esos namespaces en Application: 0 resultados | 1 |
| A4 | Orquesta vía puertos inyectados, nunca I/O directo | Ningún `DbContext`, `HttpClient`, `File.*` invocado directamente en esta capa | 1 |
| A5 | Las reglas de negocio del caso de uso viven en los servicios, no en los repositorios | Los repositorios son acceso a datos; las reglas de autorización/invariantes están en `Services/` | 1 |
| A6 | DTOs propios, sin filtrar entidades de Domain hacia afuera | Los métodos públicos de los servicios devuelven DTOs, no entidades de EF rastreadas | 1 |
| A7 | El genérico es la regla; un repositorio enfocado solo existe con una razón documentada | Cada `I*Repository` enfocado explica en su doc-comment qué necesidad real cubre fuera del genérico | 1 |
| A8 | Los servicios de agentes externos NO están implementados aquí | Ningún `Services/` habla directo con SMTP/HTTP de terceros/almacenamiento — solo con su propia interfaz | 1 |

---

## 5. Capa 3 — Infrastructure

### 5.1 Qué va aquí

| Carpeta | Contenido | Ejemplo |
|---|---|---|
| `Persistence/` (o `Database/`) | `DbContext`, interceptores, `Configurations/` (una `IEntityTypeConfiguration<T>` por entidad), `Migrations/` | `AppDbContext`, `OrderConfiguration` |
| `Repositories/` | Implementación del `Repository<T>` genérico + `UnitOfWork`, y de los repositorios enfocados | `Repository<T>`, `UnitOfWork`, `OrderRepository` |
| `<Servicio externo>/` — una carpeta por integración | Clientes concretos de APIs externas, almacenamiento, mensajería | `Email/`, `BlobStorage/`, `PaymentGateway/` |
| `Auth/` | Hashing de contraseñas, emisión/validación de JWT, proveedores de identidad | `Argon2PasswordHasher`, `JwtTokenService` |
| `Logging/` | Enriquecedores, políticas de enmascarado, decoradores de logging transversal | `SensitiveDataPolicy` |
| `Registration/` | Módulos de registro de DI, uno por sub-responsabilidad | `PersistenceRegistration`, `AuthRegistration` |
| `DependencyInjection.cs` | Único punto que agrega todos los módulos de `Registration/` (`AddInfrastructure()`) | — |

### 5.2 Servicios de agentes externos: por qué viven aquí, no en Application

Esta es la misma frontera de §4.3, vista desde este lado. Todo servicio cuya implementación real necesita hablar con **algo fuera del control de la aplicación** —un proveedor de correo, una pasarela de pago, un almacenamiento de archivos, una API de terceros, un sistema de mensajería— se implementa en Infrastructure, sin importar que su interfaz se haya declarado en Application. La razón es la misma que sostiene toda la Inversión de Dependencias: Application declara *qué* necesita: Infrastructure decide *cómo* se satisface con un proveedor concreto, y ese "cómo" es, por definición, un detalle de infraestructura que puede cambiar (de SendGrid a Amazon SES, de Stripe a otra pasarela) sin que el caso de uso se entere.

```csharp
// La interfaz (el "qué") se declaró en Application — ver §4.3.
// Aquí vive el "cómo": un proveedor concreto, con su SDK, sus credenciales, su formato de request.
namespace MyApp.Infrastructure.Email;

public class SendGridEmailSender(SendGridClient client) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var msg = MailHelper.CreateSingleEmail(from: FromAddress, new EmailAddress(to), subject, body, body);
        await client.SendEmailAsync(msg, ct);
    }
}
```

Una carpeta por integración (`Email/`, `PaymentGateway/`, `BlobStorage/`) mantiene cada agente externo aislado del resto — cambiar de proveedor de correo nunca debería tocar el código que sube archivos.

### 5.3 Logging transversal por decorador (AOP), no manual en cada servicio

Escribir `logger.LogInformation("Start")`/`"End"` al principio y al final de cada método de cada servicio es repetitivo, se olvida en los servicios nuevos, y termina divergiendo (un servicio loguea el resultado, otro no, un tercero solo loguea al fallar). La alternativa recomendada: **un proxy dinámico (`DispatchProxy`) que envuelve cualquier interfaz de servicio y agrega el rastro de inicio/fin/error automáticamente**, sin que el propio servicio escriba una sola línea de logging para eso.

```csharp
namespace MyApp.Infrastructure.Logging;

/// <summary>Decorador AOP genérico: envuelve CUALQUIER interfaz y traza inicio/fin/error de cada
/// método, sin una clase de decorador por servicio. Enmascara los argumentos marcados con
/// [SensitiveData] (§3.6) antes de escribirlos al log.</summary>
public class LoggingDispatchProxy : DispatchProxy
{
    private object _target = null!;
    private ILogger _logger = null!;

    internal void Configure(object target, ILogger logger) { _target = target; _logger = logger; }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        _logger.LogInformation("[{Type}.{Method}] Start", _target.GetType().Name, method!.Name);
        try
        {
            var result = method.Invoke(_target, args);
            _logger.LogInformation("[{Type}.{Method}] End", _target.GetType().Name, method.Name);
            return result;
        }
        catch (TargetInvocationException ex)
        {
            _logger.LogError(ex.InnerException, "[{Type}.{Method}] Error", _target.GetType().Name, method.Name);
            throw ex.InnerException ?? ex;
        }
    }
}
```

**Se conecta con un único registro en el Composition Root de esta capa, cubriendo TODOS los servicios de golpe** — no hace falta decorar cada interfaz a mano ni escribir una clase decoradora por servicio:
```csharp
namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddPersistence(config).AddEmailInfra(config); // registro normal de cada servicio

        // Envuelve TODA interfaz cuya implementación viva en Application o Infrastructure —
        // una sola línea cubre decenas de servicios, sin tocarlos uno por uno.
        services.DecorateWithLoggingByNamespace("MyApp.Application", "MyApp.Infrastructure");
        return services;
    }
}
```

**Regla de uso**: si un servicio necesita registrar un evento de negocio específico que el proxy genérico no puede conocer ("se aplicó un descuento del 15% por nivel Gold"), esa línea puntual se agrega dentro del método — el proxy sigue cubriendo el Start/End/Error genérico alrededor. Uno no reemplaza al otro; el proxy elimina el ruido repetitivo, no el logging con intención.

### 5.4 Qué NO va aquí, y por qué

| Nunca debe aparecer | Por qué |
|---|---|
| Interfaces propias que Application no haya definido | Infrastructure **implementa**, no **define** contratos — si necesita un contrato nuevo, se agrega primero en Application (Inversión de Dependencias) |
| Reglas de negocio | Un repositorio decide *cómo* consultar, nunca *si* una operación está permitida — esa decisión es de Application |
| Referencias de tipos de EF/Azure/etc. expuestas fuera de esta capa | Si `DbContext` o `DbSet<T>` aparecen en la firma de un método consumido desde Application o Api, la Ignorancia de Persistencia ya se rompió |

### 5.5 Especificación del proyecto

- `Infrastructure.csproj`: `ProjectReference` a Domain **y** Application (para implementar sus interfaces). Aquí viven todos los `PackageReference` "pesados": el proveedor de EF Core, SDKs de nube, librerías de criptografía, clientes HTTP de terceros.
- Ninguna otra capa tiene `ProjectReference` *hacia* Infrastructure salvo Api (y solo para el *wiring* de arranque).
- El mapeo objeto-relacional se declara con **Fluent API**, nunca con atributos sobre la entidad:

```csharp
namespace MyApp.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
    }
}
```

  Y se aplican todas por reflexión, para que agregar una entidad nueva no requiera tocar el `DbContext`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

### 5.6 El interceptor de auditoría: cómo se estampan `CreatedDate`/`UpdatedDate`

`AuditableEntity` (§3.2) declara `CreatedDate`/`UpdatedDate`, pero **ningún servicio de Application los asigna a mano** — si `OrderService.ConfirmAsync` tuviera que escribir `order.UpdatedDate = DateTime.UtcNow` antes de guardar, la regla se repetiría (y eventualmente se olvidaría) en cada método de cada servicio que modifica una entidad auditable. En vez de eso, un único punto de Infrastructure intercepta *cada* `SaveChanges`/`SaveChangesAsync` de todo el `DbContext` y estampa los campos automáticamente, sin que ningún caso de uso lo sepa ni lo pida.

**Por qué un `SaveChangesInterceptor` de EF Core y no un `override SaveChanges` dentro del propio `DbContext`**: sobrescribir `SaveChanges` directamente en la clase `AppDbContext` mezcla la responsabilidad de "ser el `DbContext`" con la de "aplicar una regla transversal de auditoría" — dos razones de cambio distintas en la misma clase (§2.6). Un interceptor separado se registra, se prueba y se reemplaza de forma independiente del `DbContext`, y EF Core lo invoca automáticamente en cada `SaveChanges`, sin que el `DbContext` necesite saber que existe.

```csharp
namespace MyApp.Infrastructure.Persistence;

/// <summary>Estampa CreatedDate/UpdatedDate en cada entidad AuditableEntity que se guarda —
/// el único lugar de toda la solución que lo hace. Usa TimeProvider (no DateTime.UtcNow
/// directo) para que un test pueda sustituir el reloj por uno falso y verificar el valor exacto.</summary>
public class AuditSaveChangesInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedDate = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedDate = now;
        }
    }
}
```

Se registra una sola vez, junto con el `DbContext`, en `PersistenceRegistration` (§5.8):
```csharp
public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
{
    services.AddSingleton<AuditSaveChangesInterceptor>();
    services.AddDbContext<AppDbContext>((sp, options) =>
        options.UseSqlServer(config.GetConnectionString("Default"))
               .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
    return services;
}
```

Con esto conectado, **ningún servicio de Application escribe jamás `CreatedDate`/`UpdatedDate`** — si algún día aparece uno haciéndolo a mano, es la señal de que el interceptor no está registrado, o de que alguien reintrodujo el patrón manual que este mecanismo existe justamente para eliminar.

### 5.7 Punto adicional (opcional): quién hizo el cambio y desde dónde

Algunos proyectos necesitan más que "cuándo" — necesitan **quién** creó/modificó el registro, y opcionalmente **desde dónde** (IP del cliente, nombre de la máquina de origen), típicamente por trazabilidad regulatoria, soporte de incidentes, o auditoría de seguridad. Esto no reemplaza a `AuditableEntity` (§3.2) — lo extiende, y **solo para las entidades donde el negocio realmente lo necesita**, no como valor por defecto de todas.

Esta es la única funcionalidad de este documento que, para funcionar completa, atraviesa las 4 capas a la vez. El desglose de qué va en cada una:

| Capa | Qué agrega | Por qué ahí |
|---|---|---|
| **Domain** | `FullyAuditableEntity` (hereda de `AuditableEntity`) con `CreatedByUserId`/`UpdatedByUserId`/`CreatedByIp`/`UpdatedByIp`/`CreatedByMachine`/`UpdatedByMachine` | Son campos que las entidades declaran e inheredan, igual que `CreatedDate`/`UpdatedDate` (§3.2) — tipos simples, cero dependencias |
| **Application** | El puerto `ICurrentUser`, con `UserId`/`IpAddress`/`MachineName` | Application (y el interceptor de Infrastructure) necesitan preguntar "¿quién y desde dónde?" sin saber CÓMO se resuelve esa respuesta — Inversión de Dependencias (§2.2) |
| **Infrastructure** | `CurrentUserContext` (implementación concreta, mutable, *scoped* por solicitud) que implementa `ICurrentUser`; y la extensión del interceptor de §5.6 para leerlo | La implementación real es un detalle técnico; el interceptor es quien consume el puerto para estampar los campos, igual que ya hace con `CreatedDate`/`UpdatedDate` |
| **Api** | `CurrentUserMiddleware`, que llena `CurrentUserContext` a partir del `HttpContext` (claims del JWT para el usuario, `RemoteIpAddress` para la IP) | Solo Api puede tocar `HttpContext` — Ignorancia de Presentación (§2.4). Fuera de un contexto HTTP (un job, una consola) `CurrentUserContext` simplemente queda en sus valores por defecto, y el interceptor no estampa esos campos — no rompe nada |

```csharp
// Domain — MyApp.Domain.Common (extiende AuditableEntity, §3.2)
public abstract class FullyAuditableEntity : AuditableEntity
{
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UpdatedByIp { get; set; }
    public string? CreatedByMachine { get; set; }
    public string? UpdatedByMachine { get; set; }
}
```
```csharp
// Application — MyApp.Application.Interfaces.Services (el puerto)
public interface ICurrentUser
{
    int? UserId { get; }
    string? IpAddress { get; }
    string? MachineName { get; }
}
```
```csharp
// Infrastructure — MyApp.Infrastructure.Auth (implementación concreta y mutable, scoped)
public class CurrentUserContext : ICurrentUser
{
    public int? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? MachineName { get; set; }
}
```
```csharp
// Infrastructure — extensión del AuditSaveChangesInterceptor de §5.6
public class AuditSaveChangesInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    // ... SavingChanges/SavingChangesAsync delegan a Stamp() igual que en §5.6 ...

    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedDate = now;
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedDate = now;
        }

        // Solo entra aquí si la entidad además hereda FullyAuditableEntity — el resto ni lo evalúa.
        foreach (var entry in context.ChangeTracker.Entries<FullyAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedByUserId = currentUser.UserId;
                entry.Entity.CreatedByIp = currentUser.IpAddress;
                entry.Entity.CreatedByMachine = currentUser.MachineName;
            }
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedByUserId = currentUser.UserId;
                entry.Entity.UpdatedByIp = currentUser.IpAddress;
                entry.Entity.UpdatedByMachine = currentUser.MachineName;
            }
        }
    }
}
```
```csharp
// Api — MyApp.Api.Middleware (el único lugar que toca HttpContext para esto)
public class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CurrentUserContext currentUser)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            currentUser.UserId = int.Parse(context.User.FindFirst("sub")!.Value);

        currentUser.IpAddress = context.Connection.RemoteIpAddress?.ToString();
        currentUser.MachineName = context.Request.Headers["X-Client-Machine"].FirstOrDefault();

        await next(context);
    }
}
```

Registro (Infrastructure expone la instancia concreta *scoped* como el puerto de Application):
```csharp
services.AddScoped<CurrentUserContext>();
services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());
```

Con esto, `Order : FullyAuditableEntity` queda con auditoría completa sin que `OrderService` (Application) escriba una sola línea para ello — exactamente la misma garantía que `AuditableEntity` ya daba para `CreatedDate`/`UpdatedDate`, ahora extendida a "quién" y "desde dónde".

### 5.8 Ejemplos

**Repositorio genérico**:
```csharp
namespace MyApp.Infrastructure.Repositories;

public class Repository<T>(AppDbContext db) : IRepository<T> where T : class, IEntity
{
    public Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Set<T>().FindAsync([id], ct).AsTask();

    public async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default) =>
        await db.Set<T>().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await db.Set<T>().Where(predicate).ToListAsync(ct);

    public Task AddAsync(T entity, CancellationToken ct = default) => db.Set<T>().AddAsync(entity, ct).AsTask();
    public void Update(T entity) => db.Set<T>().Update(entity);
    public void Remove(T entity) => db.Set<T>().Remove(entity);
}
```

**Unit of Work con caché por tipo**:
```csharp
namespace MyApp.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    public IRepository<T> Repository<T>() where T : class, IEntity
    {
        if (!_repositories.TryGetValue(typeof(T), out var repo))
            _repositories[typeof(T)] = repo = new Repository<T>(db);
        return (IRepository<T>)repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

Para el módulo de registro `PersistenceRegistration` (uno por sub-responsabilidad, agregado en `AddInfrastructure`, incluyendo el interceptor de auditoría), ver §5.6.

### 5.9 Checklist de auditoría — Infrastructure

| # | Criterio | Cómo verificarlo | Peso |
|---|---|---|---|
| I1 | Depende de Application+Domain; nadie interno depende de ella | `ProjectReference` correctas; 0 referencias inversas | 1 |
| I2 | No declara interfaces propias | 0 archivos `I*` cuyo contrato no venga ya de Application | 1 |
| I3 | Concentra todo lo específico de tecnología/vendor | EF, SDKs de nube, criptografía, clientes HTTP — todo aquí | 1 |
| I4 | Ignorancia de persistencia hacia arriba | `DbContext`/`DbSet<T>` nunca en una firma pública consumida fuera de esta capa | 1 |
| I5 | Una `IEntityTypeConfiguration<T>` por entidad, aplicadas por ensamblado | Conteo de configuraciones == conteo de entidades mapeadas; `ApplyConfigurationsFromAssembly` presente | 1 |
| I6 | Los servicios de agentes externos están implementados aquí, no en Application | Cada integración externa (correo, pagos, storage) tiene su implementación concreta en una carpeta propia de esta capa | 1 |

---

## 6. Capa 4 — Presentation / Api (Composition Root)

### 6.1 Qué va aquí

| Carpeta | Contenido | Ejemplo |
|---|---|---|
| `Controllers/` | Un controller por recurso/agregado — delgados, sin lógica de negocio | `OrdersController` |
| `Contracts/` | DTOs de transporte HTTP — **propios**, no reutilizados de Application | `CreateOrderRequest`, `OrderResponse` |
| `Middleware/` | Manejo centralizado de excepciones, identidad del usuario actual | `ExceptionMiddleware`, `CurrentUserMiddleware` |
| `Filters/` | Cross-cutting a nivel de acción: logging, validación de modelo | `ValidationActionFilter` |
| `Validators/` | Validación del contrato de transporte (si se usa FluentValidation u similar) | `CreateOrderRequestValidator` |
| `Extensions/` | Métodos de extensión que agrupan configuración de arranque por *concern* | `AddSwaggerDocs()`, `AddJwtAuth()` |
| `Program.cs` | El **Composition Root**: conecta `AddApplication()` + `AddInfrastructure()` + los `Extensions/` de esta capa, y define el pipeline HTTP | — |

### 6.2 Por qué Api tiene su propio DTO y no reutiliza el de Application

Este es el punto que más se pasa por alto, y por eso merece su propia sección.

Un DTO de Application (`OrderDto`) representa la forma que un *caso de uso* necesita. Un contrato de Api (`OrderResponse`) representa la forma que el *cliente HTTP* necesita — que casi siempre coincide, pero no por obligación: puede ocultar campos internos, renombrar propiedades por convención de la API pública, o formatear fechas como string ISO. Si un controller devuelve directamente el DTO de Application:

- Un cambio interno en Application (agregar un campo para un caso de uso nuevo) se convierte automáticamente, y sin que nadie lo note, en un cambio del contrato público de la API.
- Application termina, en la práctica, versionado por compatibilidad HTTP — exactamente lo que la Ignorancia de Presentación prohíbe.

La duplicación que esto genera cuando ambas formas coinciden byte a byte es **el costo correcto a pagar**, no un error de diseño.

### 6.3 Cómo invocar los validadores en el borde HTTP: Action Filter, no auto-validación

El paquete histórico de integración automática de FluentValidation con MVC (`FluentValidation.AspNetCore`, que inyectaba la validación directamente en el *model binding*) está **deprecado desde FluentValidation 12**, y nunca funcionó con Minimal API. Cualquier tutorial que lo recomiende está desactualizado. La práctica vigente, confirmada por la documentación oficial de FluentValidation: un **filtro de acción (o *endpoint filter* en Minimal API) que resuelve `IValidator<T>` desde el contenedor de DI, valida el argumento de la acción después del *model binding*, y corta la ejecución con una respuesta 400 si falla** — sin repetir esas mismas líneas en cada controller.

```csharp
namespace MyApp.Api.Filters;

/// <summary>Filtro global: valida cada argumento de la acción que tenga un IValidator&lt;T&gt;
/// registrado. Los tipos sin validador quedan sin efecto — no hace falta registrar nada por endpoint.</summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (!result.IsValid)
                throw new DomainValidationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
        }

        await next();
    }
}
```

Registro, una sola vez, en el Composition Root:
```csharp
builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>());
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly); // registra todos los IValidator<T>
```

`DomainValidationException` es, igual que `NotFoundException` (§3.3), una `DomainException` más — el filtro lanza la misma excepción que un Servicio de Aplicación lanzaría si se invocara sin pasar por Api (ver §4.6). El `ExceptionMiddleware` (§6.4) ya la captura y la traduce a `400 Bad Request`, sin necesitar una rama aparte.

Con esto, la petición HTTP queda protegida por el filtro antes de llegar al controller, y el mismo caso de uso queda protegido por el servicio (§4.6) si se invoca desde cualquier otro punto de entrada — ninguno de los dos sustituye al otro.

### 6.4 Manejo centralizado de excepciones: por qué evita el try-catch disperso

Sin un middleware central, cada controller (o cada servicio) necesitaría su propio `try/catch` para traducir cada tipo de `DomainException` a un código HTTP — repetido en cada acción, y roto en cuanto alguien olvida agregar un `catch` nuevo. **La alternativa: un único middleware, registrado una sola vez, que intercepta cualquier excepción de toda la aplicación y la traduce.** Ningún controller ni servicio necesita un `try/catch` para este propósito — solo lanzan la excepción (§3.3) y siguen con su lógica.

Para que agregar un tipo de excepción nuevo sea de una sola línea, en vez de un bloque `catch` más, la traducción se resuelve con una tabla/`switch` de tipo → código HTTP — no con una cadena creciente de bloques `catch`:

```csharp
namespace MyApp.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            var statusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ForbiddenException => StatusCodes.Status403Forbidden,
                ConflictException => StatusCodes.Status409Conflict,
                InsufficientStockException => StatusCodes.Status409Conflict,
                DomainValidationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest // cualquier DomainException nueva cae aquí por defecto
            };

            logger.LogWarning(ex, "[ExceptionMiddleware] {StatusCode} {Message}", statusCode, ex.Message);
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex) // cualquier excepción NO prevista — nunca debería filtrar detalles internos
        {
            logger.LogError(ex, "[ExceptionMiddleware] Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
        }
    }
}
```

Agregar una excepción de negocio nueva —`DuplicateEmailException`, digamos— es una línea nueva en el `switch`, no un bloque `catch` adicional ni un cambio en ningún controller. Y ningún servicio de Application necesita saber que existen los códigos 404/409/400 — eso es, por completo, un detalle de Api (Ignorancia de Presentación, §2.4).

### 6.5 Qué NO va aquí, y por qué

| Nunca debe aparecer | Por qué |
|---|---|
| Lógica de negocio en un controller | El controller solo traduce HTTP ↔ caso de uso; la regla vive en Application |
| Un controller que referencia una clase concreta de Infrastructure | Rompe la Inversión de Dependencias — debe depender de la interfaz de Application, igual que cualquier otro consumidor |
| Registros de DI dispersos fuera de `Program.cs`/`Extensions/` | Rompe el Composition Root único — dificulta saber, con una sola lectura, todo lo que la aplicación registra |

### 6.6 Ejemplos

**Controller delgado**:
```csharp
namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<OrderResponse>> Confirm(int id, CancellationToken ct)
    {
        var dto = await orders.ConfirmAsync(id, ct);
        return Ok(new OrderResponse(dto.Id, dto.CustomerName, dto.Status.ToString(), dto.TotalAmount));
    }
}
```

**Contrato de transporte propio** (nótese: independiente del `OrderDto` de Application):
```csharp
namespace MyApp.Api.Contracts;

public record OrderResponse(int Id, string CustomerName, string Status, decimal TotalAmount);
```

Para el middleware de excepciones (el único lugar de toda la solución donde el tipo concreto de una `DomainException` se traduce a un código HTTP), ver §6.4.

**Composition Root**:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);      // capa Application
builder.Services.AddInfrastructure(builder.Configuration);   // capa Infrastructure
builder.Services.AddControllers();
builder.Services.AddSwaggerDocs();                            // extraído a Api/Extensions

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.Run();
```

### 6.7 Checklist de auditoría — Api

| # | Criterio | Cómo verificarlo | Peso |
|---|---|---|---|
| P1 | Composition Root único | `Program.cs` conecta `AddApplication()`+`AddInfrastructure()`; sin `services.Add*` dispersos en otros archivos | 1 |
| P2 | Controllers delgados | Cada acción delega a Application en ≤ 2-3 líneas de traducción HTTP | 1 |
| P3 | Contratos propios, separados de los DTOs de Application | `Api/Contracts` (o equivalente) no reutiliza tipos de `Application/DTOs` como forma pública | 1 |
| P4 | Concerns transversales centralizados | Excepciones, autenticación, logging y validación resueltos en middleware/filtros, no repetidos por controller | 1 |
| P5 | Ningún controller referencia tipos concretos de Infrastructure | `grep` de namespaces de Infrastructure en `Controllers/`: 0 resultados (Infrastructure solo se toca en el Composition Root) | 1 |
| P6 | El Composition Root está libre de lógica de negocio | `Program.cs` no contiene ramas condicionales de negocio ni orquesta múltiples servicios más allá del *wiring* | 1 |

---

## 7. El grafo de dependencias como prueba de auditoría

La forma más objetiva y difícil de falsear de validar este patrón es leer, no adivinar, las referencias de proyecto:

```
Domain.csproj          → (sin ProjectReference)
Application.csproj     → Domain.csproj
Infrastructure.csproj  → Domain.csproj, Application.csproj
Api.csproj             → Application.csproj, Infrastructure.csproj
```

Cualquier flecha que no aparezca en este diagrama, o que apunte en sentido contrario (p. ej. `Application.csproj → Infrastructure.csproj`), es una violación de la Regla de Dependencia — sin excepción, y sin necesidad de leer una sola línea de código para detectarla.

---

## 8. Rúbrica de auditoría consolidada

Sumar los criterios de las 4 capas (§3.10, §4.10, §5.9, §6.7) da un total de **25 criterios**. Cada uno se califica:

| Valor | Significado |
|---|---|
| **1.0 — Cumple** | El patrón está implementado exactamente como se describe |
| **0.5 — Parcial** | La idea está resuelta, pero con un desvío menor (ubicación distinta, un caso aislado) |
| **0.0 — No cumple** | El criterio no se cumple o se resuelve de forma opuesta al patrón |

**Cumplimiento total = (suma de puntos) / 25.**

| Capa | Criterios | Máximo |
|---|---|---|
| Domain / Core | D1–D5 | 5 |
| Application | A1–A8 | 8 |
| Infrastructure | I1–I6 | 6 |
| Api | P1–P6 | 6 |
| **Total** | | **25** |

> Nota importante al aplicar esta rúbrica a un proyecto real: **no confundir "no sigue esta rúbrica" con "no sigue el patrón Onion"**. Elecciones de tecnología (¿usa AutoMapper?, ¿tiene un bus de mensajes?, ¿FluentValidation vive en Application o en Api?) son convenciones de un producto o equipo específico, no reglas del patrón. Esta rúbrica mide únicamente lo que el patrón exige — dirección de dependencias, ignorancia de framework, separación de contratos, composition root único. Un proyecto puede diferir en convenciones de estilo de otro proyecto "de referencia" y aun así cumplir el 100% de esta rúbrica.

---

## 9. Anti-patrones comunes (qué buscar primero en una auditoría)

| Anti-patrón | Síntoma | Por qué rompe el patrón |
|---|---|---|
| **Modelo dual de entidades** | Existen clases "de dominio" y clases "de EF" separadas, con métodos `MapToEntity()`/`MapToDomain()` | Señala que en algún momento se intentó mantener Domain "puro" agregando una capa de mapeo en vez de simplemente no ensuciar la entidad real — genera duplicación y deriva |
| **Repositorio-God / Facade ad-hoc** | Una sola interfaz con 40-60 métodos que mezcla CRUD de todas las entidades | Imposible de testear con foco, esconde qué depende de qué; se resuelve con `IRepository<T>` genérico + repositorios enfocados por agregado cuando de verdad se necesita lógica extra |
| **Entidades con atributos de EF/JSON** | `[Column]`, `[JsonPropertyName]` sobre una clase de `Entities/` | Ignorancia de persistencia rota; el mapeo debe vivir en Infrastructure vía Fluent API |
| **DTO de Application usado como respuesta HTTP directa** | Un controller retorna `Ok(applicationDto)` sin traducción | Acopla el contrato público a la forma interna — ver §6.2 |
| **Interfaces en Infrastructure** | Un archivo `I*.cs` declarado junto a su única implementación, en Infrastructure | Invierte la Inversión de Dependencias: la capa externa termina dictando el contrato en vez de implementarlo |
| **Program.cs de cientos de líneas** | Bloques de configuración inline en vez de métodos de extensión con nombre | El Composition Root deja de ser legible como un mapa de "qué se registra" |
| **Colisión de nombres Entity/DTO** | `Order` (entidad) y `Order` (DTO) obligan a un alias `using OrderEntity = ...` en cada archivo que usa ambos | Señala que el DTO no se distingue léxicamente del dominio — resolver con un sufijo consistente (`OrderDto`) |
| **Lógica de negocio en un middleware o filtro de Api** | Un filtro decide reglas específicas del dominio (no genéricas de transporte) | El filtro es un mecanismo de infraestructura HTTP; las reglas de negocio pertenecen a Application |
| **"Servicio de Application" que en realidad llama un SDK externo** | Una clase en `Application/Services/` con `using SendGrid;`/`using Stripe.net;`/un `HttpClient` apuntando a un proveedor de terceros | Rompe A3/A8 (§4.10) — esa implementación pertenece a Infrastructure (§5.2); Application solo debería conocer la interfaz |
| **Un método público de un servicio devuelve la entidad, no un DTO** | `Task<Order> GetAsync(int id)` en vez de `Task<OrderDto> GetAsync(int id)` | Filtra el estado mutable/rastreado por EF hacia afuera del caso de uso — viola A6 (§4.10); ver §4.4 |
| **Enum comparado por su representación en texto** | `status.ToString() == "Confirmed"` o `statusString == "Confirmed"` en una condición de negocio | Frágil ante typos, cambios de idioma o renombres silenciosos — la comparación debe ser por el valor del enum (§3.5); el literal solo va en un mensaje |
| **Número mágico o string repetido sin constante con nombre** | `if (attempts >= 5)` repetido en 3 archivos distintos, cada uno con un número "5" suelto | Nadie puede cambiar la regla en un solo lugar, y el "5" no comunica qué representa — debe ser una constante nombrada (§3.4) |
| **Un campo sensible sin `[SensitiveData]` termina en un log** | `logger.LogInformation("{@user}", user)` serializa `PasswordHash`/`RefreshToken` completos al log o a un servicio externo (Application Insights, Datadog) | El dato sensible sale del proceso sin que nadie lo note — debe marcarse en Domain (§3.6) para que el logging (manual o AOP, §5.3) lo enmascare |
| **Logging manual repetido en cada servicio (`Start`/`End` a mano)** | Cada método de cada servicio empieza y termina con las mismas 2 líneas de `logger.LogInformation` | Se olvida en servicios nuevos y diverge entre equipos — un decorador AOP (§5.3) lo cubre una sola vez para toda una capa |
| **Cadena creciente de `catch` en el middleware de excepciones** | `catch (NotFoundException)`, `catch (ConflictException)`, `catch (ForbiddenException)`… un bloque nuevo por cada excepción de negocio | No escala: cada excepción nueva exige tocar el middleware con un bloque más — un `switch` de tipo → código HTTP (§6.4) resuelve lo mismo en una línea |
| **`CreatedDate`/`UpdatedDate` asignados a mano en un servicio** | `order.UpdatedDate = DateTime.UtcNow;` antes de un `SaveChangesAsync()` dentro de un Servicio de Aplicación | Repite una regla transversal en cada método que modifica una entidad auditable, y eventualmente alguien la olvida — debe estamparla un único `SaveChangesInterceptor` (§5.6), nunca el caso de uso |

---

## 10. Cómo usar este documento

1. **Para un proyecto nuevo**: usar §3–§6 como plantilla de carpetas antes de escribir la primera línea de código de negocio.
2. **Para auditar un proyecto existente**:
   a. Reconstruir el grafo de dependencias real (§7) leyendo los `.csproj` — no asumir a partir de nombres de carpeta.
   b. Recorrer la rúbrica (§8) capa por capa, con evidencia concreta (grep, lectura de archivos) para cada criterio — nunca marcar "Cumple" por impresión general.
   c. Revisar la lista de anti-patrones (§9) como barrido inicial — suelen concentrar la mayoría de los hallazgos.
   d. Reportar el cumplimiento por capa y el total, y separar explícitamente los hallazgos que violan el patrón de los que son solo una diferencia de convención frente a "cómo lo hace otro proyecto" (ver nota en §8).
3. **Para comparar dos proyectos entre sí** (p. ej. un proyecto nuevo contra uno de referencia de la organización): ejecutar la auditoría del punto 2 sobre ambos, y además comparar convenciones específicas del equipo (nombres de carpeta, librerías elegidas) como un análisis *separado* del cumplimiento del patrón — no mezclar ambos números.

---

*Documento de referencia genérico — aplicable a cualquier proyecto .NET (ASP.NET Core Web API, Worker Service, etc.) independientemente de su dominio de negocio.*

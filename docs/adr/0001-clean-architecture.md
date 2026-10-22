# 0001 · Clean Architecture ligera, sin MediatR ni AutoMapper

**Estado:** Aceptada

## Contexto

La API tiene reglas de negocio claras (stock, SKU, roles) y debe poder probarse sin base de datos ni HTTP. A la vez es un sistema pequeño: unos 15 endpoints y 4 casos de uso principales. Hacía falta separar responsabilidades sin agregar ceremonia que no aporta a este tamaño.

## Decisión

Cuatro proyectos con las dependencias apuntando hacia el dominio:

```
Api ──► Application ──► Domain
Infrastructure ──► Application
```

- **Domain:** entidades (`Product`, `Movement`), objetos de valor (`Sku`, `Quantity`) y excepciones de negocio. No depende de ningún paquete.
- **Application:** casos de uso como clases simples (`ProductService`, `MovementService`, `StockService`, `LoginService`) y las interfaces que necesita (`IProductRepository`, `ITokenService`, `IUserContext`…).
- **Infrastructure:** implementa esas interfaces (Dapper, BCrypt, JWT).
- **Api:** HTTP, contratos (DTOs), autenticación y traducción de errores.

Sin **MediatR**: un controller llama directamente a un servicio de aplicación. Sin **AutoMapper**: los contratos tienen métodos `From(...)` explícitos.

Una prueba de arquitectura (`LayerDependencyTests`) falla si una capa interna referencia a una externa.

## Alternativas consideradas

- **Un solo proyecto con carpetas:** más simple, pero nada impide que un controller use SQL directamente y las reglas terminan mezcladas.
- **CQRS con MediatR:** útil con muchos casos de uso o comportamientos transversales (*pipelines*). Aquí agregaría una indirección por cada endpoint sin beneficio real.
- **AutoMapper:** ahorra líneas, pero esconde el mapeo y los errores aparecen en ejecución. Con pocos DTOs, el mapeo explícito es más legible y lo valida el compilador.

## Consecuencias

- Las reglas del dominio y los casos de uso se prueban con *fakes*, en milisegundos.
- Cambiar de motor de base de datos o de ORM solo toca Infrastructure.
- Hay un poco más de código de mapeo manual, a cambio de que sea explícito y verificable al compilar.

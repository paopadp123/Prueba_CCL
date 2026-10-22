# Decisiones de arquitectura (ADR)

Un **ADR** (*Architecture Decision Record*) documenta una decisión técnica importante: el problema que había, qué se eligió, qué alternativas se descartaron y qué consecuencias tiene. Sirve para que quien llegue al proyecto entienda **por qué** el código es como es, no solo **qué** hace.

Formato de cada documento: **Estado · Contexto · Decisión · Alternativas consideradas · Consecuencias**.

| # | Decisión | Estado |
|---|---|---|
| [0001](0001-clean-architecture.md) | Clean Architecture ligera, sin MediatR ni AutoMapper | Aceptada |
| [0002](0002-stock-en-procedimientos-almacenados.md) | Reglas de stock en procedimientos almacenados con bloqueo de fila | Aceptada |
| [0003](0003-dapper-en-lugar-de-ef-core.md) | Dapper en lugar de Entity Framework Core | Aceptada |
| [0004](0004-auditoria-con-triggers.md) | Auditoría con triggers en la base de datos | Aceptada |
| [0005](0005-errores-con-problem-details.md) | Errores con ProblemDetails y códigos estables | Aceptada |
| [0006](0006-autenticacion-jwt.md) | Autenticación JWT sin estado y token en sessionStorage | Aceptada |
| [0007](0007-movimientos-inmutables-y-baja-logica.md) | Movimientos inmutables y baja lógica de productos | Aceptada |
| [0008](0008-frontend-angular-sin-libreria-ui.md) | Frontend Angular moderno sin librería de componentes | Aceptada |

# 0006 · Autenticación JWT sin estado y token en sessionStorage

**Estado:** Aceptada

## Contexto

La prueba pide autenticación con JWT. Hay dos roles: `Admin` gestiona el catálogo y `Operator` consulta y registra movimientos. El front es una SPA en otro origen durante el desarrollo, y en producción va detrás del mismo dominio.

## Decisión

**API**
- `POST /api/auth/login` verifica la contraseña con **BCrypt** y emite un **JWT HS256 de 60 minutos** con `sub`, `unique_name`, `name`, `role` y `jti`.
- El login responde igual **y tarda lo mismo** si el usuario no existe, está inactivo o la clave es incorrecta: cuando el usuario no existe, verifica contra un hash ficticio. Así no se puede averiguar qué usuarios existen.
- Se valida emisor, audiencia, firma y vencimiento, con una tolerancia de reloj de 30 segundos.
- **Política por defecto:** todo endpoint exige autenticación salvo que diga `[AllowAnonymous]`; las escrituras del catálogo exigen `[Authorize(Roles = "Admin")]`.
- **Límite de intentos:** 5 logins por minuto por IP (`429` con `Retry-After`).
- La clave de firma no está en el repositorio (user-secrets o variables de entorno) y se valida al iniciar que tenga al menos 32 caracteres.

**Front**
- El token se guarda en **sessionStorage**: sobrevive a recargar la página, pero se borra al cerrar la pestaña.
- Un interceptor agrega `Authorization: Bearer` **solo** a las peticiones dirigidas a la API propia.
- La sesión se cierra sola al vencer el token, o ante cualquier `401`.

## Alternativas consideradas

- **Cookies de sesión en el servidor:** obligan a guardar estado y a protegerse de CSRF; el JWT sin estado escala sin memoria compartida.
- **Cookie `HttpOnly` con el JWT:** más resistente a XSS, porque JavaScript no puede leerla, pero requiere protección CSRF y configuración de dominio. Queda como mejora.
- **localStorage:** el token sobreviviría indefinidamente en el equipo, incluso después de cerrar el navegador.
- **Refresh tokens:** útiles para sesiones largas, pero agregan rotación, revocación y almacenamiento. Para esta prueba, volver a iniciar sesión cada 60 minutos es aceptable.

## Consecuencias

- No se puede revocar un token antes de que venza; se mitiga con la duración corta.
- Cualquier almacenamiento accesible desde JavaScript es vulnerable si hay XSS. Se mitiga con el escape automático de Angular, sin `innerHTML` con datos del usuario.
- Los *guards* del front son comodidad, no seguridad: la autorización real la decide siempre la API.

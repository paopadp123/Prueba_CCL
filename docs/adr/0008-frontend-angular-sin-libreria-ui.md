# 0008 · Frontend Angular moderno sin librería de componentes

**Estado:** Aceptada

## Contexto

La prueba pide Angular 20, una interfaz responsiva y manejo de errores visible para el usuario. Las pantallas son pocas (login, inicio, productos, movimientos, stock), pero deben verse bien en celular y en escritorio.

## Decisión

- **Angular 20 con componentes standalone, signals y sin zone.js** (`provideZonelessChangeDetection`):
  - El estado se maneja con `signal` y `computed`.
  - RxJS se usa para lo asíncrono: HTTP, `debounceTime` en la búsqueda, `switchMap` para cancelar búsquedas viejas.
  - `toSignal` une los dos mundos.
- **Lazy loading en todas las rutas** (`loadComponent` y `loadChildren`), con los guards en `canMatch` para que el código protegido ni siquiera se descargue sin sesión.
- **Estructura por funcionalidad:** `core/` (sesión, interceptores, layout, tema), `shared/` (piezas reutilizables) y `features/` (una carpeta por pantalla).
- **Sin librería de componentes** (Angular Material, PrimeNG…):
  - Estilos propios en SCSS, con todos los colores en variables CSS.
  - Íconos SVG propios con `currentColor`.
  - Gráficos del tablero en SVG.
- **Diseño responsivo:** en celular, menú hamburguesa con panel lateral, y las tablas se convierten en tarjetas con CSS Grid.
- **Modo claro, oscuro o del sistema:** redefiniendo las variables CSS bajo `[data-theme='dark']`, con un script en `index.html` que aplica el tema antes de pintar para evitar el destello.
- **Variables de entorno:** `apiUrl` apunta a `http://localhost:5197` en desarrollo y queda vacía en producción, donde nginx sirve el front y la API en el mismo origen.

## Alternativas consideradas

- **Angular Material:** componentes accesibles listos para usar, pero con más peso, un estilo reconocible difícil de adaptar al diseño buscado y otra dependencia que actualizar junto con Angular.
- **Tailwind CSS:** rápido para prototipar, pero agrega una herramienta de build y clases largas en las plantillas.
- **Librería de gráficos (Chart.js, ngx-charts):** se justificaría con gráficos interactivos complejos; para tres gráficos simples, SVG propio pesa casi nada.
- **Zone.js (el modo clásico):** funciona, pero con signals ya no es necesario y agrega peso y ciclos de detección de cambios que no hacen falta.

## Consecuencias

- La carga inicial pesa unos 84 kB comprimida, y cada pantalla es un archivo aparte que se descarga al entrar.
- La accesibilidad (foco, `aria-*`, navegación con teclado, contraste) es responsabilidad propia; se cuidó en los menús, formularios y gráficos, y se cubre con pruebas.
- Cambiar el aspecto visual, o agregar otro tema, es tocar variables CSS, no componentes.

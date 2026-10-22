import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guards';

/**
 * Cada pantalla se carga con `loadComponent` (lazy loading): su código se descarga
 * solo cuando el usuario navega a ella, así la carga inicial es más liviana.
 * Las secciones con varias pantallas usan `loadChildren` para traer su archivo de rutas.
 */
export const routes: Routes = [
  {
    path: 'login',
    canMatch: [guestGuard],
    title: 'Iniciar sesión · Inventario',
    loadComponent: () => import('./features/login/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    canMatch: [authGuard],
    loadComponent: () => import('./core/layout/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'Inicio · Inventario',
        loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
      },
      {
        path: 'products',
        loadChildren: () => import('./features/products/products.routes').then((m) => m.productRoutes),
      },
      {
        path: 'movements',
        loadChildren: () => import('./features/movements/movements.routes').then((m) => m.movementRoutes),
      },
      {
        path: 'stock',
        title: 'Stock · Inventario',
        loadComponent: () => import('./features/stock/stock-page').then((m) => m.StockPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

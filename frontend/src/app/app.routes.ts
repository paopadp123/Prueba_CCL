import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guards';

/**
 * Cada pantalla se carga con `loadComponent` (lazy loading): su código se descarga
 * solo cuando el usuario navega a ella, así la carga inicial es más liviana.
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
        title: 'Inicio · Inventario',
        loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

import { Routes } from '@angular/router';

/**
 * Rutas hijas de /movements. Admin y Operator registran movimientos.
 */
export const movementRoutes: Routes = [
  {
    path: '',
    title: 'Movimientos · Inventario',
    loadComponent: () => import('./movements-page').then((m) => m.MovementsPage),
  },
  {
    path: 'new',
    title: 'Registrar movimiento · Inventario',
    loadComponent: () => import('./movement-form-page').then((m) => m.MovementFormPage),
  },
];

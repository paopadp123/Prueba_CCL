import { Routes } from '@angular/router';

import { adminGuard } from '../../core/auth/auth.guards';

/**
 * Rutas hijas de /products. Se cargan juntas con `loadChildren`, y cada pantalla
 * a su vez es un chunk aparte con `loadComponent`.
 */
export const productRoutes: Routes = [
  {
    path: '',
    title: 'Productos · Inventario',
    loadComponent: () => import('./products-list-page').then((m) => m.ProductsListPage),
  },
  {
    path: 'new',
    canMatch: [adminGuard],
    title: 'Nuevo producto · Inventario',
    loadComponent: () => import('./product-form-page').then((m) => m.ProductFormPage),
  },
  {
    path: ':id/edit',
    canMatch: [adminGuard],
    title: 'Editar producto · Inventario',
    loadComponent: () => import('./product-form-page').then((m) => m.ProductFormPage),
  },
  {
    path: ':id',
    title: 'Producto · Inventario',
    loadComponent: () => import('./product-detail-page').then((m) => m.ProductDetailPage),
  },
];

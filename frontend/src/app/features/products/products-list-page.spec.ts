import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { PagedResponse } from '../../shared/data/paged';
import { Product } from './product.models';
import { ProductsListPage } from './products-list-page';
import { ProductsService } from './products.service';

const page: PagedResponse<Product> = {
  items: [
    {
      id: 1,
      sku: 'LAP-001',
      name: 'Portátil',
      description: null,
      unitPrice: 1500,
      currentStock: 4,
      isActive: true,
      createdAt: '2026-10-21T10:00:00Z',
      updatedAt: '2026-10-21T10:00:00Z',
    },
  ],
  page: 1,
  pageSize: 10,
  totalCount: 1,
  totalPages: 1,
};

describe('ProductsListPage', () => {
  const isAdmin = signal(false);
  let fixture: ComponentFixture<ProductsListPage>;
  let products: jasmine.SpyObj<ProductsService>;

  async function render(admin: boolean): Promise<HTMLElement> {
    isAdmin.set(admin);
    products = jasmine.createSpyObj<ProductsService>('ProductsService', ['list', 'setActive']);
    products.list.and.returnValue(of(page));

    await TestBed.configureTestingModule({
      imports: [ProductsListPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: ProductsService, useValue: products },
        { provide: AuthService, useValue: { isAdmin } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductsListPage);
    await new Promise((resolve) => setTimeout(resolve, 300));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('muestra los productos de la API', async () => {
    const element = await render(false);

    expect(element.textContent).toContain('LAP-001');
    expect(element.textContent).toContain('Portátil');
    expect(products.list).toHaveBeenCalledWith(jasmine.objectContaining({ page: 1, includeInactive: false }));
  });

  it('un operador no ve las acciones de administración', async () => {
    const element = await render(false);

    expect(element.textContent).not.toContain('Nuevo producto');
    expect(element.querySelector('[aria-label="Editar Portátil"]')).toBeNull();
    expect(element.textContent).not.toContain('Desactivar');
  });

  it('un administrador ve crear, editar y desactivar', async () => {
    const element = await render(true);

    expect(element.textContent).toContain('Nuevo producto');
    expect(element.querySelector('[aria-label="Editar Portátil"]')).not.toBeNull();
    expect(element.textContent).toContain('Desactivar');
  });
});

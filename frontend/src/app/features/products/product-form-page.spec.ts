import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ApiError } from '../../core/http/api-error';
import { Product } from './product.models';
import { ProductFormPage } from './product-form-page';
import { ProductsService } from './products.service';

const saved: Product = {
  id: 7,
  sku: 'LAP-001',
  name: 'Portátil',
  description: null,
  unitPrice: 1500,
  currentStock: 0,
  isActive: true,
  createdAt: '2026-10-21T10:00:00Z',
  updatedAt: '2026-10-21T10:00:00Z',
};

describe('ProductFormPage', () => {
  let fixture: ComponentFixture<ProductFormPage>;
  let products: jasmine.SpyObj<ProductsService>;
  let navigate: jasmine.Spy;

  async function render(id?: string): Promise<HTMLElement> {
    products = jasmine.createSpyObj<ProductsService>('ProductsService', ['get', 'create', 'update']);
    products.get.and.returnValue(of(saved));

    await TestBed.configureTestingModule({
      imports: [ProductFormPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), { provide: ProductsService, useValue: products }],
    }).compileComponents();

    navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(ProductFormPage);
    if (id) {
      fixture.componentRef.setInput('id', id);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function type(element: HTMLElement, selector: string, value: string): void {
    const input = element.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submit(element: HTMLElement): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('crea el producto con el SKU en mayúsculas y abre su ficha', async () => {
    const element = await render();
    products.create.and.returnValue(of(saved));

    type(element, '#sku', 'lap-001');
    type(element, '#name', 'Portátil');
    type(element, '#unitPrice', '1500');
    await submit(element);

    expect(products.create).toHaveBeenCalledWith({
      sku: 'LAP-001',
      name: 'Portátil',
      description: null,
      unitPrice: 1500,
    });
    expect(navigate).toHaveBeenCalledWith(['/products', 7]);
  });

  it('con datos inválidos no llama a la API', async () => {
    const element = await render();

    type(element, '#sku', '-x');
    await submit(element);

    expect(products.create).not.toHaveBeenCalled();
    expect(element.textContent).toContain('De 3 a 30 caracteres');
  });

  it('muestra el SKU duplicado junto al campo', async () => {
    const element = await render();
    products.create.and.returnValue(
      throwError(() => new ApiError(409, 'Ya existe un producto con el SKU LAP-001.', 'duplicate_sku')),
    );

    type(element, '#sku', 'LAP-001');
    type(element, '#name', 'Portátil');
    type(element, '#unitPrice', '10');
    await submit(element);

    expect(element.textContent).toContain('Ya existe un producto con el SKU LAP-001.');
  });

  it('al editar carga el producto y bloquea el SKU', async () => {
    const element = await render('7');

    expect(products.get).toHaveBeenCalledWith(7);
    const sku = element.querySelector<HTMLInputElement>('#sku')!;
    expect(sku.value).toBe('LAP-001');
    expect(sku.disabled).toBeTrue();
  });
});

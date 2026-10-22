import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ApiError } from '../../core/http/api-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { Product } from '../products/product.models';
import { ProductsService } from '../products/products.service';
import { MovementFormPage } from './movement-form-page';
import { MovementsService } from './movements.service';

const product: Product = {
  id: 3,
  sku: 'LAP-001',
  name: 'Portátil',
  description: null,
  unitPrice: 1500,
  currentStock: 5,
  isActive: true,
  createdAt: '2026-10-21T10:00:00Z',
  updatedAt: '2026-10-21T10:00:00Z',
};

describe('MovementFormPage', () => {
  let fixture: ComponentFixture<MovementFormPage>;
  let movements: jasmine.SpyObj<MovementsService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  async function render(type: string, productId: string): Promise<HTMLElement> {
    movements = jasmine.createSpyObj<MovementsService>('MovementsService', ['register']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    const products = jasmine.createSpyObj<ProductsService>('ProductsService', ['list']);
    products.list.and.returnValue(of({ items: [product], page: 1, pageSize: 200, totalCount: 1, totalPages: 1 }));

    await TestBed.configureTestingModule({
      imports: [MovementFormPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: MovementsService, useValue: movements },
        { provide: ProductsService, useValue: products },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MovementFormPage);
    fixture.componentRef.setInput('type', type);
    fixture.componentRef.setInput('productId', productId);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  async function submitQuantity(element: HTMLElement, quantity: string): Promise<void> {
    const input = element.querySelector<HTMLInputElement>('#quantity')!;
    input.value = quantity;
    input.dispatchEvent(new Event('input'));
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('llega con el tipo y el producto elegidos y muestra el stock actual', async () => {
    const element = await render('Exit', '3');

    expect(element.querySelector<HTMLInputElement>('input[value="Exit"]')!.checked).toBeTrue();
    expect(element.textContent).toContain('Stock actual: 5');
    expect(element.textContent).toContain('Registrar salida');
  });

  it('avisa antes de enviar si la salida supera el stock conocido', async () => {
    const element = await render('Exit', '3');

    const input = element.querySelector<HTMLInputElement>('#quantity')!;
    input.value = '8';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(element.textContent).toContain('supera el stock actual');
  });

  it('muestra el 409 de stock insuficiente junto a la cantidad', async () => {
    const element = await render('Exit', '3');
    movements.register.and.returnValue(
      throwError(() => new ApiError(409, 'Stock insuficiente: disponible 5, solicitado 8.', 'insufficient_stock')),
    );

    await submitQuantity(element, '8');

    expect(movements.register).toHaveBeenCalledWith('Exit', { productId: 3, quantity: 8, reason: null });
    expect(element.textContent).toContain('Stock insuficiente: disponible 5, solicitado 8.');
  });

  it('al registrar avisa, actualiza el stock mostrado y limpia la cantidad', async () => {
    const element = await render('Entry', '3');
    movements.register.and.returnValue(
      of({ movementId: 1, productId: 3, type: 'Entry' as const, quantity: 10, stockAfter: 15 }),
    );

    await submitQuantity(element, '10');

    expect(notifications.success).toHaveBeenCalledWith('Entrada registrada: Portátil. Stock actual: 15.');
    expect(element.textContent).toContain('Stock actual: 15');
    expect(element.querySelector<HTMLInputElement>('#quantity')!.value).toBe('');
  });
});

export type MovementType = 'Entry' | 'Exit';

export interface RegisterMovementRequest {
  productId: number;
  quantity: number;
  reason: string | null;
}

export interface RegisteredMovement {
  movementId: number;
  productId: number;
  type: MovementType;
  quantity: number;
  stockAfter: number;
}

export interface Movement {
  movementId: number;
  productId: number;
  sku: string;
  productName: string;
  type: MovementType;
  quantity: number;
  stockAfter: number;
  reason: string | null;
  username: string;
  createdAt: string;
}

export const movementTypeLabels: Record<MovementType, string> = {
  Entry: 'Entrada',
  Exit: 'Salida',
};

export const reasonMaxLength = 250;

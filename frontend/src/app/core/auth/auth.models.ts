export type Role = 'Admin' | 'Operator';

export interface User {
  id: number;
  username: string;
  fullName: string;
  role: Role;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  user: User;
}

/**
 * Sesión guardada en el navegador: el token y cuándo vence.
 */
export interface Session {
  accessToken: string;
  expiresAt: string;
  user: User;
}

import type { UserDto } from './types';

// JWT + user are kept in localStorage so auth survives across the Module Federation
// boundary (shell and every remote read the same browser-global storage).
const TOKEN_KEY = 'urp.token';
const USER_KEY = 'urp.user';

export function setSession(token: string, user: UserDto): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getUser(): UserDto | null {
  const raw = localStorage.getItem(USER_KEY);
  return raw ? (JSON.parse(raw) as UserDto) : null;
}

export function isAuthenticated(): boolean {
  return !!getToken();
}

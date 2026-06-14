import React, { createContext, useCallback, useContext, useState } from 'react';
import { api, clearSession, getUser, setSession, type AuthResult, type UserDto } from '@urp/shared';

interface AuthContextValue {
  user: UserDto | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue>(null!);

export const useAuth = () => useContext(AuthContext);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(getUser());

  const login = useCallback(async (email: string, password: string) => {
    const result = await api.post<AuthResult>('/auth/login', { email, password });
    setSession(result.token, result.user);
    setUser(result.user);
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
  }, []);

  return <AuthContext.Provider value={{ user, login, logout }}>{children}</AuthContext.Provider>;
}

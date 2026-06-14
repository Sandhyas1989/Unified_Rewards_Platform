import React from 'react';
import { Button, RoleLabels } from '@urp/shared';
import { useAuth } from './AuthContext';

const PORTAL_LABELS = ['Employee Portal', 'Manager Portal', 'HR Portal', 'Finance Portal'];

export function Layout({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  return (
    <div>
      <header className="urp-shell__header">
        <div className="urp-shell__brand">
          Unified Rewards <small>{user ? PORTAL_LABELS[user.role] : ''}</small>
        </div>
        <div className="urp-shell__user">
          <span className="urp-shell__pill">{user ? RoleLabels[user.role] : ''}</span>
          <span>{user?.fullName}</span>
          <Button variant="ghost" onClick={logout}>Sign out</Button>
        </div>
      </header>
      <main className="urp-shell__main">{children}</main>
    </div>
  );
}

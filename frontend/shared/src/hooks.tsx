import React, { useEffect, useState } from 'react';
import { api } from './api';
import type { UserDto } from './types';

/** Loads the user directory once (requires Manager/HR/Finance role). */
export function useUsers() {
  const [users, setUsers] = useState<UserDto[]>([]);
  useEffect(() => {
    api.getItems<UserDto>('/employees?pageSize=200').then(setUsers).catch(() => setUsers([]));
  }, []);
  const byId = (id?: string) => users.find((u) => u.id === id);
  return { users, byId };
}

/** Employee/user picker backed by the user directory. */
export function UserSelect({
  value,
  onChange,
  filterRole,
  placeholder = 'Select an employee…',
}: {
  value: string;
  onChange: (id: string) => void;
  filterRole?: number;
  placeholder?: string;
}) {
  const { users } = useUsers();
  const list = filterRole === undefined ? users : users.filter((u) => u.role === filterRole);
  return (
    <select value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">{placeholder}</option>
      {list.map((u) => (
        <option key={u.id} value={u.id}>{u.fullName} — {u.email}</option>
      ))}
    </select>
  );
}

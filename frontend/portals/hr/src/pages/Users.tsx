import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, Field, RoleLabels, type UserDto,
} from '@urp/shared';

export function Users() {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('Password123!');
  const [grade, setGrade] = useState('E1');
  const [dateOfJoining, setDateOfJoining] = useState(new Date().toISOString().slice(0, 10));
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setErr('');
    try { setUsers(await api.getItems<UserDto>('/employees?pageSize=200')); }
    catch (e: any) { setErr(e.message); }
  };
  useEffect(() => { load(); }, []);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg(''); setBusy(true);
    try {
      await api.post('/employees', { fullName, email, password, grade, dateOfJoining, managerId: null });
      setMsg('Employee created.');
      setFullName(''); setEmail('');
      load();
    } catch (e: any) { setErr(e.message); }
    finally { setBusy(false); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />
      <Card title="Create employee">
        <form onSubmit={submit}>
          <Field label="Full name"><input value={fullName} onChange={(e) => setFullName(e.target.value)} required /></Field>
          <Field label="Email"><input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></Field>
          <Field label="Temp password"><input value={password} onChange={(e) => setPassword(e.target.value)} required /></Field>
          <Field label="Grade"><input value={grade} onChange={(e) => setGrade(e.target.value)} required /></Field>
          <Field label="Date of joining"><input type="date" value={dateOfJoining} onChange={(e) => setDateOfJoining(e.target.value)} required /></Field>
          <Button type="submit" disabled={busy}>{busy ? 'Creating…' : 'Create employee'}</Button>
        </form>
      </Card>
      <Card title="Directory">
        <Table
          rows={users}
          columns={[
            { header: 'Name', render: (u) => u.fullName },
            { header: 'Email', render: (u) => u.email },
            { header: 'Role', render: (u) => <Badge>{RoleLabels[u.role]}</Badge> },
            { header: 'Active', render: (u) => (u.isActive ? '✓' : '✗') },
          ]}
        />
      </Card>
    </div>
  );
}

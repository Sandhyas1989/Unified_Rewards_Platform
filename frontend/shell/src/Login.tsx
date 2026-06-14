import React, { useState } from 'react';
import { Banner, Button, Field, ApiError } from '@urp/shared';
import { useAuth } from './AuthContext';

export function Login() {
  const { login } = useAuth();
  const [email, setEmail] = useState('employee@urp.local');
  const [password, setPassword] = useState('Password123!');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login(email, password);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Login failed. Is the API running on :5287?');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="urp-login">
      <form className="urp-login__card" onSubmit={submit}>
        <h1>Unified Rewards</h1>
        <p>Sign in to your rewards portal</p>
        <Banner kind="error" message={error} />
        <Field label="Email">
          <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required />
        </Field>
        <Field label="Password">
          <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" required />
        </Field>
        <Button type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</Button>
        <div className="urp-login__hint">
          Demo accounts (password <code>Password123!</code>):<br />
          employee@urp.local · manager@urp.local · hr@urp.local · finance@urp.local
        </div>
      </form>
    </div>
  );
}

import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, Field, money,
  ClaimStatusLabels, ClaimTypeLabels,
  type ClaimDto,
} from '@urp/shared';

const STATUS_TONE = ['warn', 'warn', 'good', 'bad', 'good'] as const;

export function Claims() {
  const [claims, setClaims] = useState<ClaimDto[]>([]);
  const [type, setType] = useState(0);
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setErr('');
    try { setClaims(await api.get<ClaimDto[]>('/claims/me')); }
    catch (e: any) { setErr(e.message); }
  };
  useEffect(() => { load(); }, []);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg(''); setBusy(true);
    try {
      await api.post('/claims', { type, amount: Number(amount), description });
      setMsg('Claim submitted.');
      setAmount(''); setDescription('');
      load();
    } catch (e: any) { setErr(e.message); }
    finally { setBusy(false); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />

      <Card title="Submit a claim">
        <form onSubmit={submit}>
          <Field label="Type">
            <select value={type} onChange={(e) => setType(Number(e.target.value))}>
              {ClaimTypeLabels.map((l, i) => <option key={i} value={i}>{l}</option>)}
            </select>
          </Field>
          <Field label="Amount">
            <input type="number" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} required />
          </Field>
          <Field label="Description">
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} required />
          </Field>
          <Button type="submit" disabled={busy}>{busy ? 'Submitting…' : 'Submit claim'}</Button>
        </form>
      </Card>

      <Card title="My claims">
        <Table
          rows={claims}
          empty="No claims submitted yet."
          columns={[
            { header: 'Type', render: (c) => ClaimTypeLabels[c.type] },
            { header: 'Amount', render: (c) => money(c.amount) },
            { header: 'Description', render: (c) => c.description },
            { header: 'Status', render: (c) => <Badge tone={STATUS_TONE[c.status]}>{ClaimStatusLabels[c.status]}</Badge> },
          ]}
        />
      </Card>
    </div>
  );
}

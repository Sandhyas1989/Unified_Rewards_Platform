import React, { useEffect, useRef, useState } from 'react';
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
  const [currencyCode, setCurrencyCode] = useState('INR');
  const [description, setDescription] = useState('');
  const [receipt, setReceipt] = useState<File | null>(null);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

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
      const claim = await api.post<ClaimDto>('/claims', { type, amount: Number(amount), description, currencyCode });

      if (receipt) {
        const form = new FormData();
        form.append('ClaimId', claim.id);
        form.append('File', receipt);
        await api.postForm('/documents', form);
      }

      setMsg(receipt ? 'Claim submitted with receipt.' : 'Claim submitted.');
      setAmount(''); setDescription(''); setCurrencyCode('INR'); setReceipt(null);
      if (fileRef.current) fileRef.current.value = '';
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
          <Field label="Currency">
            <select value={currencyCode} onChange={(e) => setCurrencyCode(e.target.value)}>
              <option value="INR">INR — Indian Rupee</option>
              <option value="USD">USD — US Dollar</option>
              <option value="EUR">EUR — Euro</option>
              <option value="GBP">GBP — British Pound</option>
              <option value="SGD">SGD — Singapore Dollar</option>
            </select>
          </Field>
          <Field label="Description">
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} required />
          </Field>
          <Field label="Receipt (optional)">
            <input
              ref={fileRef}
              type="file"
              accept="image/*,application/pdf"
              onChange={(e) => setReceipt(e.target.files?.[0] ?? null)}
            />
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
            { header: 'Amount', render: (c) => money(c.amount, c.currencyCode) },
            { header: 'Description', render: (c) => c.description },
            { header: 'Status', render: (c) => <Badge tone={STATUS_TONE[c.status]}>{ClaimStatusLabels[c.status]}</Badge> },
          ]}
        />
      </Card>
    </div>
  );
}

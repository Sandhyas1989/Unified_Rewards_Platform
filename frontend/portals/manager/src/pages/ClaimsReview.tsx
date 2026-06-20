import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, Field, money, useUsers,
  ClaimStatusLabels, ClaimTypeLabels, type ClaimDto,
} from '@urp/shared';

const TONE = ['warn', 'warn', 'good', 'bad', 'good'] as const;

export function ClaimsReview() {
  const [claims, setClaims] = useState<ClaimDto[]>([]);
  const [status, setStatus] = useState('');
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const { byId } = useUsers();

  const load = async () => {
    setErr('');
    try {
      const q = status === '' ? '?pageSize=100' : `?status=${status}&pageSize=100`;
      setClaims(await api.getItems<ClaimDto>(`/claims${q}`));
    } catch (e: any) { setErr(e.message); }
  };
  useEffect(() => { load(); }, [status]);

  const act = async (id: string, verb: 'approve' | 'reject') => {
    setErr(''); setMsg('');
    try {
      await api.post(`/claims/${id}/${verb}`, { notes: '' });
      setMsg(`Claim ${verb.replace('-', ' ')} succeeded.`);
      load();
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />
      <Card
        title="Claims awaiting action"
        actions={
          <Field label="">
            <select value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="">All statuses</option>
              {ClaimStatusLabels.map((l, i) => <option key={i} value={i}>{l}</option>)}
            </select>
          </Field>
        }
      >
        <Table
          rows={claims}
          empty="No claims to review."
          columns={[
            { header: 'Employee', render: (c) => byId(c.employeeId)?.fullName ?? c.employeeId.slice(0, 8) },
            { header: 'Type', render: (c) => ClaimTypeLabels[c.type] },
            { header: 'Amount', render: (c) => money(c.amount, c.currencyCode) },
            { header: 'Status', render: (c) => <Badge tone={TONE[c.status]}>{ClaimStatusLabels[c.status]}</Badge> },
            {
              header: 'Actions',
              render: (c) => (c.status === 0 || c.status === 1) ? (
                <span style={{ display: 'flex', gap: 6 }}>
                  <Button variant="primary" onClick={() => act(c.id, 'approve')}>Approve</Button>
                  <Button variant="danger" onClick={() => act(c.id, 'reject')}>Reject</Button>
                </span>
              ) : <span className="urp-empty">—</span>,
            },
          ]}
        />
      </Card>
    </div>
  );
}

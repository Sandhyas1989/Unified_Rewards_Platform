import React, { useEffect, useState } from 'react';
import { api, Card, Banner, Table, Field } from '@urp/shared';

interface AuditEntryDto {
  id: string;
  eventType: string;
  claimId: string;
  actorId?: string;
  amount?: number;
  notes?: string;
  occurredAtUtc: string;
}

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

const EVENT_LABELS: Record<string, string> = {
  ClaimSubmitted: 'Claim Submitted',
  ClaimApproved: 'Claim Approved',
  ClaimRejected: 'Claim Rejected',
  ClaimSettled: 'Claim Settled',
  BonusAwarded: 'Bonus Awarded',
};

export function Audit() {
  const [entries, setEntries] = useState<AuditEntryDto[]>([]);
  const [claimId, setClaimId] = useState('');
  const [err, setErr] = useState('');
  const [total, setTotal] = useState(0);

  const load = async (filter?: string) => {
    setErr('');
    try {
      const path = filter
        ? `/audit?claimId=${filter}&pageSize=100`
        : '/audit?pageSize=100';
      const result = await api.get<PagedResult<AuditEntryDto>>(path);
      setEntries(result.items);
      setTotal(result.totalCount);
    } catch (e: any) { setErr(e.message); }
  };

  useEffect(() => { load(); }, []);

  const search = (e: React.FormEvent) => {
    e.preventDefault();
    load(claimId.trim() || undefined);
  };

  return (
    <div>
      <Banner kind="error" message={err} />

      <Card title="Audit Trail">
        <form onSubmit={search} style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-end', marginBottom: '1rem' }}>
          <Field label="Filter by Claim ID">
            <input
              type="text"
              placeholder="Paste a claim UUID…"
              value={claimId}
              onChange={(e) => setClaimId(e.target.value)}
              style={{ width: '22rem' }}
            />
          </Field>
          <button type="submit" className="urp-btn urp-btn--primary">Search</button>
          <button type="button" className="urp-btn urp-btn--ghost"
            onClick={() => { setClaimId(''); load(); }}>Clear</button>
        </form>

        <p style={{ color: '#666', fontSize: '0.85rem', marginBottom: '0.75rem' }}>
          Showing {entries.length} of {total} audit entries (newest first)
        </p>

        <Table
          rows={entries}
          empty="No audit entries recorded yet. Claim lifecycle events will appear here as they occur."
          columns={[
            { header: 'Event', render: (a) => EVENT_LABELS[a.eventType] ?? a.eventType },
            { header: 'Claim ID', render: (a) => <code style={{ fontSize: '0.75rem' }}>{a.claimId.slice(0, 8)}…</code> },
            { header: 'Actor ID', render: (a) => a.actorId ? <code style={{ fontSize: '0.75rem' }}>{a.actorId.slice(0, 8)}…</code> : '—' },
            { header: 'Amount', render: (a) => a.amount != null ? `₹${a.amount.toLocaleString('en-IN')}` : '—' },
            { header: 'Notes', render: (a) => a.notes ?? '—' },
            {
              header: 'When (UTC)',
              render: (a) => new Date(a.occurredAtUtc).toLocaleString('en-IN', {
                day: '2-digit', month: 'short', year: 'numeric',
                hour: '2-digit', minute: '2-digit',
              }),
            },
          ]}
        />
      </Card>
    </div>
  );
}

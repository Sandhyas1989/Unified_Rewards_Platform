import React, { useEffect, useState } from 'react';
import { api, Card, Button, Table, Badge, Banner, Field, useUsers } from '@urp/shared';
import type { PromotionDto, NominationDto } from '@urp/shared';

const OUTCOME_LABELS = ['Pending', 'Awarded', 'Not Awarded', 'Withdrawn'];
const OUTCOME_TONE = ['warn', 'good', 'bad', 'neutral'] as const;

export function Promotions() {
  const [promotions, setPromotions] = useState<PromotionDto[]>([]);
  const [selected, setSelected] = useState<PromotionDto | null>(null);
  const [nominations, setNominations] = useState<NominationDto[]>([]);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [nomEmployeeId, setNomEmployeeId] = useState('');
  const [remarks, setRemarks] = useState('');
  const [busy, setBusy] = useState(false);
  const { users } = useUsers();

  const eligibleEmployees = users.filter(u => u.role === 0 || u.role === 1);

  const load = async () => {
    try {
      const r = await api.get<{ items: PromotionDto[] }>('/promotions?pageSize=100');
      setPromotions(r.items.filter(p => p.status === 1));   // only Open campaigns
    } catch (e: any) { setErr(e.message); }
  };

  const loadNominations = async (id: string) => {
    try {
      const r = await api.get<NominationDto[]>(`/promotions/${id}/nominations`);
      setNominations(r);
    } catch (e: any) { setErr(e.message); }
  };

  useEffect(() => { load(); }, []);

  const nominate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selected || !nomEmployeeId) return;
    setErr(''); setMsg(''); setBusy(true);
    try {
      await api.post(`/promotions/${selected.id}/nominations`, { employeeId: nomEmployeeId, remarks: remarks || null });
      setMsg('Employee nominated successfully.');
      setNomEmployeeId(''); setRemarks('');
      loadNominations(selected.id);
      load();
    } catch (e: any) { setErr(e.message); }
    finally { setBusy(false); }
  };

  const select = (p: PromotionDto) => { setSelected(p); loadNominations(p.id); };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />

      <Card title="Open Bonus Campaigns">
        <Table
          rows={promotions}
          empty="No open bonus campaigns at the moment."
          columns={[
            { header: 'Title', render: p => <span style={{ cursor: 'pointer', color: '#185FA5' }} onClick={() => select(p)}>{p.title}</span> },
            { header: 'Cycle', render: p => `${p.cycleYear} ${p.cycleQuarter}` },
            { header: 'Eligible Grade | Bonus', render: p => `${p.fromGrade || 'All'} | ₹${p.bonusValue.toLocaleString()}` },
            { header: 'Window', render: p => `${p.nominationStart} to ${p.nominationEnd}` },
            { header: 'Nominations', render: p => p.nominationCount },
            { header: '', render: p => <Button variant="primary" onClick={() => select(p)}>Nominate</Button> },
          ]}
        />
      </Card>

      {selected && (
        <>
          <Card title={`Nominate for: ${selected.title}`}>
            <p style={{ color: '#555', marginBottom: '0.75rem', fontSize: '0.875rem' }}>
              {selected.fromGrade
                ? <>Eligible grade: <strong>{selected.fromGrade}</strong> — only employees at this grade will pass eligibility.</>
                : 'All grades are eligible for this campaign.'}
              {' '}Bonus payout: <strong>₹{selected.bonusValue.toLocaleString()}</strong> per approved nominee.
            </p>
            <form onSubmit={nominate}>
              <Field label="Select Employee">
                <select required value={nomEmployeeId} onChange={e => setNomEmployeeId(e.target.value)}>
                  <option value="">Select an employee…</option>
                  {eligibleEmployees.map(u => (
                    <option key={u.id} value={u.id}>{u.fullName} — {u.email}</option>
                  ))}
                </select>
              </Field>
              <Field label="Remarks (optional)">
                <textarea value={remarks} onChange={e => setRemarks(e.target.value)} rows={2} />
              </Field>
              <Button type="submit" disabled={busy}>{busy ? 'Submitting…' : 'Submit Nomination'}</Button>
            </form>
          </Card>

          <Card title={`Existing Nominations — ${selected.title}`}>
            <Table
              rows={nominations}
              empty="No nominations yet for this campaign."
              columns={[
                { header: 'Employee', render: n => n.employeeName ?? n.employeeId.slice(0, 8) },
                { header: 'Nominated On', render: n => n.nominatedOn },
                { header: 'Outcome', render: n => <Badge tone={OUTCOME_TONE[n.outcome]}>{OUTCOME_LABELS[n.outcome]}</Badge> },
                { header: 'Remarks', render: n => n.remarks ?? '—' },
              ]}
            />
          </Card>
        </>
      )}
    </div>
  );
}

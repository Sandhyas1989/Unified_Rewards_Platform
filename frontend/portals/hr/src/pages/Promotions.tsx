import React, { useEffect, useState } from 'react';
import { api, Card, Button, Table, Badge, Banner, Field } from '@urp/shared';
import type { PromotionDto, NominationDto } from '@urp/shared';

const STATUS_LABELS = ['Draft', 'Open', 'Closed', 'Cancelled'];
const STATUS_TONE = ['warn', 'good', 'neutral', 'bad'] as const;
const OUTCOME_LABELS = ['Pending', 'Awarded', 'Not Awarded', 'Withdrawn'];
const OUTCOME_TONE = ['warn', 'good', 'bad', 'neutral'] as const;
const QUARTERS = ['Q1', 'Q2', 'Q3', 'Q4'];

export function Promotions() {
  const [promotions, setPromotions] = useState<PromotionDto[]>([]);
  const [selected, setSelected] = useState<PromotionDto | null>(null);
  const [nominations, setNominations] = useState<NominationDto[]>([]);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [showCreate, setShowCreate] = useState(false);

  // Create form state
  const [title, setTitle] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [quarter, setQuarter] = useState('Q4');
  const [fromGrade, setFromGrade] = useState('');
  const [bonusValue, setBonusValue] = useState('');
  const [nomStart, setNomStart] = useState('');
  const [nomEnd, setNomEnd] = useState('');
  const [minTenure, setMinTenure] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    try {
      const r = await api.get<{ items: PromotionDto[] }>('/promotions?pageSize=100');
      setPromotions(r.items);
    } catch (e: any) { setErr(e.message); }
  };

  const loadNominations = async (promoId: string) => {
    try {
      const r = await api.get<NominationDto[]>(`/promotions/${promoId}/nominations`);
      setNominations(r);
    } catch (e: any) { setErr(e.message); }
  };

  useEffect(() => { load(); }, []);

  const create = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg(''); setBusy(true);
    try {
      await api.post('/promotions', {
        title, cycleYear: year, cycleQuarter: quarter,
        fromGrade,
        bonusValue: Number(bonusValue),
        nominationStart: nomStart, nominationEnd: nomEnd,
        minTenureMonths: minTenure ? Number(minTenure) : null,
      });
      setMsg('Bonus campaign created.');
      setShowCreate(false);
      setTitle(''); setFromGrade(''); setBonusValue(''); setNomStart(''); setNomEnd(''); setMinTenure('');
      load();
    } catch (e: any) { setErr(e.message); }
    finally { setBusy(false); }
  };

  const transition = async (id: string, action: 'open' | 'close' | 'cancel') => {
    setErr(''); setMsg('');
    try {
      await api.post(`/promotions/${id}/${action}`);
      setMsg(`Cycle ${action}ed.`);
      load();
      if (selected?.id === id) {
        const updated = await api.get<PromotionDto>(`/promotions/${id}`);
        setSelected(updated);
      }
    } catch (e: any) { setErr(e.message); }
  };

  const decide = async (promoId: string, nomId: string, action: 'approve' | 'reject') => {
    setErr(''); setMsg('');
    try {
      await api.post(`/promotions/${promoId}/nominations/${nomId}/${action}`);
      setMsg(`Nomination ${action}d.`);
      loadNominations(promoId);
      load();
    } catch (e: any) { setErr(e.message); }
  };

  const select = (p: PromotionDto) => { setSelected(p); loadNominations(p.id); };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />

      <Card title="Bonus Campaigns" actions={<Button onClick={() => setShowCreate(!showCreate)}>+ New Campaign</Button>}>
        {showCreate && (
          <form onSubmit={create} style={{ background: '#f9f9f9', padding: '1rem', marginBottom: '1rem', borderRadius: '6px' }}>
            <Field label="Title"><input required value={title} onChange={e => setTitle(e.target.value)} placeholder="e.g. Year-End Bonus 2025" style={{ width: '100%' }} /></Field>
            <div style={{ display: 'flex', gap: '1rem' }}>
              <Field label="Year"><input type="number" required value={year} onChange={e => setYear(Number(e.target.value))} style={{ width: '6rem' }} /></Field>
              <Field label="Quarter"><select value={quarter} onChange={e => setQuarter(e.target.value)}>{QUARTERS.map(q => <option key={q}>{q}</option>)}</select></Field>
            </div>
            <div style={{ display: 'flex', gap: '1rem' }}>
              <Field label="Eligible Grade (optional)"><input value={fromGrade} onChange={e => setFromGrade(e.target.value)} placeholder="E2 (blank = all)" style={{ width: '10rem' }} /></Field>
              <Field label="Bonus Amount (₹)"><input type="number" required min="0" step="0.01" value={bonusValue} onChange={e => setBonusValue(e.target.value)} style={{ width: '10rem' }} /></Field>
            </div>
            <div style={{ display: 'flex', gap: '1rem' }}>
              <Field label="Nominations Open"><input type="date" required value={nomStart} onChange={e => setNomStart(e.target.value)} /></Field>
              <Field label="Nominations Close"><input type="date" required value={nomEnd} onChange={e => setNomEnd(e.target.value)} /></Field>
            </div>
            <Field label="Min Tenure (months, optional)"><input type="number" value={minTenure} onChange={e => setMinTenure(e.target.value)} style={{ width: '8rem' }} /></Field>
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem' }}>
              <Button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Create'}</Button>
              <Button type="button" variant="ghost" onClick={() => setShowCreate(false)}>Cancel</Button>
            </div>
          </form>
        )}

        <Table
          rows={promotions}
          empty="No bonus campaigns yet."
          columns={[
            { header: 'Title', render: p => <span style={{ cursor: 'pointer', color: '#185FA5' }} onClick={() => select(p)}>{p.title}</span> },
            { header: 'Cycle', render: p => `${p.cycleYear} ${p.cycleQuarter}` },
            { header: 'Eligible Grade | Bonus', render: p => `${p.fromGrade || 'All'} | ₹${p.bonusValue.toLocaleString()}` },
            { header: 'Status', render: p => <Badge tone={STATUS_TONE[p.status]}>{STATUS_LABELS[p.status]}</Badge> },
            { header: 'Nominations', render: p => `${p.nominationCount} (${p.approvedCount} awarded)` },
            {
              header: 'Actions', render: p => (
                <div style={{ display: 'flex', gap: '0.25rem' }}>
                  {p.status === 0 && <Button variant="primary" onClick={() => transition(p.id, 'open')}>Open</Button>}
                  {p.status === 1 && <Button variant="ghost" onClick={() => transition(p.id, 'close')}>Close</Button>}
                  {(p.status === 0 || p.status === 1) && <Button variant="danger" onClick={() => transition(p.id, 'cancel')}>Cancel</Button>}
                </div>
              )
            },
          ]}
        />
      </Card>

      {selected && (
        <Card title={`Nominations — ${selected.title}`}>
          <p style={{ color: '#666', marginBottom: '0.75rem' }}>
            {selected.cycleYear} {selected.cycleQuarter} · Eligible Grade: {selected.fromGrade || 'All'} · Bonus: ₹{selected.bonusValue.toLocaleString()} ·{' '}
            <Badge tone={STATUS_TONE[selected.status]}>{STATUS_LABELS[selected.status]}</Badge>
          </p>
          <Table
            rows={nominations}
            empty="No nominations for this campaign yet."
            columns={[
              { header: 'Employee', render: n => n.employeeName ?? n.employeeId.slice(0, 8) },
              { header: 'Nominated On', render: n => n.nominatedOn },
              { header: 'Outcome', render: n => <Badge tone={OUTCOME_TONE[n.outcome]}>{OUTCOME_LABELS[n.outcome]}</Badge> },
              { header: 'Remarks', render: n => n.remarks ?? '—' },
              {
                header: 'Actions', render: n => n.outcome === 0 ? (
                  <div style={{ display: 'flex', gap: '0.25rem' }}>
                    <Button variant="primary" onClick={() => decide(selected.id, n.id, 'approve')}>Award</Button>
                    <Button variant="danger" onClick={() => decide(selected.id, n.id, 'reject')}>Reject</Button>
                  </div>
                ) : null
              },
            ]}
          />
        </Card>
      )}
    </div>
  );
}

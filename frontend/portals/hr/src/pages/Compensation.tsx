import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, Field, money, UserSelect,
  GradeBandLabels, CompensationStatusLabels, type CompensationStructureDto,
} from '@urp/shared';

export function Compensation() {
  const [employeeId, setEmployeeId] = useState('');
  const [grade, setGrade] = useState(0);
  const [annualBasic, setAnnualBasic] = useState('');
  const [effectiveFrom, setEffectiveFrom] = useState(new Date().toISOString().slice(0, 10));
  const [structures, setStructures] = useState<CompensationStructureDto[]>([]);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  const loadFor = async (id: string) => {
    setErr('');
    if (!id) { setStructures([]); return; }
    try { setStructures(await api.get<CompensationStructureDto[]>(`/compensation?employeeId=${id}`)); }
    catch (e: any) { setErr(e.message); }
  };
  useEffect(() => { loadFor(employeeId); }, [employeeId]);

  const generate = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg(''); setBusy(true);
    try {
      await api.post('/compensation', { employeeId, grade, annualBasic: Number(annualBasic), effectiveFrom });
      setMsg('Compensation structure generated (Draft).');
      setAnnualBasic('');
      loadFor(employeeId);
    } catch (e: any) { setErr(e.message); }
    finally { setBusy(false); }
  };

  const approve = async (id: string) => {
    setErr(''); setMsg('');
    try {
      await api.post(`/compensation/${id}/approve`);
      setMsg('Compensation approved.');
      loadFor(employeeId);
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />
      <Card title="Employee">
        <Field label="Select employee"><UserSelect value={employeeId} onChange={setEmployeeId} /></Field>
      </Card>

      {employeeId && (
        <Card title="Generate compensation (rules engine)">
          <form onSubmit={generate}>
            <Field label="Grade band">
              <select value={grade} onChange={(e) => setGrade(Number(e.target.value))}>
                {GradeBandLabels.map((g, i) => <option key={i} value={i}>{g}</option>)}
              </select>
            </Field>
            <Field label="Annual basic"><input type="number" value={annualBasic} onChange={(e) => setAnnualBasic(e.target.value)} required /></Field>
            <Field label="Effective from"><input type="date" value={effectiveFrom} onChange={(e) => setEffectiveFrom(e.target.value)} required /></Field>
            <Button type="submit" disabled={busy}>{busy ? 'Generating…' : 'Generate'}</Button>
          </form>
        </Card>
      )}

      {structures.map((s) => (
        <Card
          key={s.id}
          title={`${GradeBandLabels[s.grade]} · effective ${s.effectiveFrom}`}
          actions={
            s.status === 0
              ? <Button variant="primary" onClick={() => approve(s.id)}>Approve</Button>
              : <Badge tone="good">{CompensationStatusLabels[s.status]}</Badge>
          }
        >
          <div className="urp-grid" style={{ marginBottom: 16 }}>
            <div className="urp-stat"><div className="urp-stat__label">Gross</div><div className="urp-stat__value">{money(s.grossAnnual)}</div></div>
            <div className="urp-stat"><div className="urp-stat__label">Deductions</div><div className="urp-stat__value">{money(s.totalDeductions)}</div></div>
            <div className="urp-stat"><div className="urp-stat__label">Net</div><div className="urp-stat__value">{money(s.netAnnual)}</div></div>
          </div>
          <Table
            rows={s.components}
            columns={[
              { header: 'Component', render: (c) => c.name },
              { header: 'Type', render: (c) => (c.type === 0 ? 'Earning' : 'Deduction') },
              { header: 'Amount', render: (c) => money(c.amount) },
            ]}
          />
        </Card>
      ))}
    </div>
  );
}

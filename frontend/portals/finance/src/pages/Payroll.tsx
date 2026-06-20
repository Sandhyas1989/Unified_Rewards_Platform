import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, Field, money, UserSelect,
  SettlementStatusLabels, type SettlementRequestDto,
} from '@urp/shared';

const TONE = ['warn', 'warn', 'good', 'bad'] as const;

export function Payroll() {
  // Payslip generation
  const [psEmployee, setPsEmployee] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [gross, setGross] = useState('');
  const [deductions, setDeductions] = useState('');
  const [net, setNet] = useState('');
  // Settlement
  const [stEmployee, setStEmployee] = useState('');
  const [amount, setAmount] = useState('');
  const [settlements, setSettlements] = useState<SettlementRequestDto[]>([]);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');

  // Poll any non-terminal settlements until they resolve.
  useEffect(() => {
    if (!settlements.some((s) => s.status < 2)) return;
    const t = setTimeout(async () => {
      const updated = await Promise.all(
        settlements.map((s) =>
          s.status < 2 ? api.get<SettlementRequestDto>(`/settlements/${s.id}`).catch(() => s) : Promise.resolve(s),
        ),
      );
      setSettlements(updated);
    }, 1500);
    return () => clearTimeout(t);
  }, [settlements]);

  const genPayslip = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg('');
    try {
      await api.post('/payslips', {
        employeeId: psEmployee, year, month,
        grossMonthly: Number(gross), totalDeductionsMonthly: Number(deductions), netMonthly: Number(net),
      });
      setMsg('Payslip generated.');
    } catch (e: any) { setErr(e.message); }
  };

  const requestSettlement = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg('');
    try {
      const s = await api.post<SettlementRequestDto>('/settlements', { employeeId: stEmployee, amount: Number(amount) });
      setMsg('Settlement queued (processing asynchronously).');
      setAmount('');
      setSettlements((prev) => [s, ...prev]);
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />

      <Card title="Generate payslip">
        <form onSubmit={genPayslip}>
          <Field label="Employee"><UserSelect value={psEmployee} onChange={setPsEmployee} /></Field>
          <Field label="Year"><input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} /></Field>
          <Field label="Month">
            <select value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </Field>
          <Field label="Gross (monthly)"><input type="number" step="0.01" value={gross} onChange={(e) => setGross(e.target.value)} required /></Field>
          <Field label="Deductions (monthly)"><input type="number" step="0.01" value={deductions} onChange={(e) => setDeductions(e.target.value)} required /></Field>
          <Field label="Net (monthly)"><input type="number" step="0.01" value={net} onChange={(e) => setNet(e.target.value)} required /></Field>
          <Button type="submit" disabled={!psEmployee}>Generate payslip</Button>
        </form>
      </Card>

      <Card title="Request settlement (async + Polly resilience)">
        <form onSubmit={requestSettlement}>
          <Field label="Employee"><UserSelect value={stEmployee} onChange={setStEmployee} /></Field>
          <Field label="Amount"><input type="number" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} required /></Field>
          <Button type="submit" disabled={!stEmployee}>Queue settlement</Button>
        </form>
        <Table
          rows={settlements}
          empty="No settlements requested this session."
          columns={[
            { header: 'Reference', render: (s) => s.reference },
            { header: 'Amount', render: (s) => money(s.amount, s.currencyCode) },
            { header: 'Status', render: (s) => <Badge tone={TONE[s.status]}>{SettlementStatusLabels[s.status]}</Badge> },
            { header: 'Confirmation', render: (s) => s.payrollConfirmation ?? (s.status < 2 ? '…' : '—') },
          ]}
        />
      </Card>
    </div>
  );
}

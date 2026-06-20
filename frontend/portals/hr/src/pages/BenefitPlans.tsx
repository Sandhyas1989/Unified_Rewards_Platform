import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, Field, money,
  BenefitCategoryLabels, type BenefitPlanDto,
} from '@urp/shared';

export function BenefitPlans() {
  const [plans, setPlans] = useState<BenefitPlanDto[]>([]);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState(0);
  const [monthlyCost, setMonthlyCost] = useState('');
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setErr('');
    try { setPlans(await api.getItems<BenefitPlanDto>('/benefit-plans?activeOnly=false&pageSize=200')); }
    catch (e: any) { setErr(e.message); }
  };
  useEffect(() => { load(); }, []);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg(''); setBusy(true);
    try {
      await api.post('/benefit-plans', { name, description, category, monthlyCost: Number(monthlyCost) });
      setMsg('Benefit plan created.');
      setName(''); setDescription(''); setMonthlyCost('');
      load();
    } catch (e: any) { setErr(e.message); }
    finally { setBusy(false); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />
      <Card title="Create benefit plan">
        <form onSubmit={submit}>
          <Field label="Name"><input value={name} onChange={(e) => setName(e.target.value)} required /></Field>
          <Field label="Description"><textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} /></Field>
          <Field label="Category">
            <select value={category} onChange={(e) => setCategory(Number(e.target.value))}>
              {BenefitCategoryLabels.map((c, i) => <option key={i} value={i}>{c}</option>)}
            </select>
          </Field>
          <Field label="Monthly cost"><input type="number" step="0.01" value={monthlyCost} onChange={(e) => setMonthlyCost(e.target.value)} required /></Field>
          <Button type="submit" disabled={busy}>{busy ? 'Creating…' : 'Create plan'}</Button>
        </form>
      </Card>
      <Card title="All plans">
        <Table
          rows={plans}
          columns={[
            { header: 'Name', render: (p) => p.name },
            { header: 'Category', render: (p) => BenefitCategoryLabels[p.category] },
            { header: 'Monthly', render: (p) => money(p.monthlyCost, p.currencyCode) },
            { header: 'Active', render: (p) => <Badge tone={p.isActive ? 'good' : 'neutral'}>{p.isActive ? 'Active' : 'Inactive'}</Badge> },
          ]}
        />
      </Card>
    </div>
  );
}

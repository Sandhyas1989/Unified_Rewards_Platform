import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Badge, Banner, money,
  BenefitCategoryLabels, EnrollmentStatusLabels,
  type BenefitPlanDto, type BenefitEnrollmentDto,
} from '@urp/shared';

export function Benefits() {
  const [plans, setPlans] = useState<BenefitPlanDto[]>([]);
  const [mine, setMine] = useState<BenefitEnrollmentDto[]>([]);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = async () => {
    setErr('');
    try {
      setPlans(await api.getItems<BenefitPlanDto>('/benefit-plans?activeOnly=true&pageSize=100'));
      setMine(await api.get<BenefitEnrollmentDto[]>('/enrollments/me'));
    } catch (e: any) {
      setErr(e.message);
    }
  };

  useEffect(() => { load(); }, []);

  const enroll = async (planId: string) => {
    setErr(''); setMsg('');
    try {
      await api.post('/enrollments', {
        benefitPlanId: planId,
        coverageStartDate: new Date().toISOString().slice(0, 10),
      });
      setMsg('Enrolled successfully.');
      load();
    } catch (e: any) { setErr(e.message); }
  };

  const cancel = async (id: string) => {
    setErr(''); setMsg('');
    try {
      await api.del(`/enrollments/${id}`);
      setMsg('Enrollment cancelled.');
      load();
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />

      <Card title="Available benefit plans">
        <Table
          rows={plans}
          columns={[
            { header: 'Plan', render: (p) => p.name },
            { header: 'Category', render: (p) => BenefitCategoryLabels[p.category] },
            { header: 'Monthly', render: (p) => money(p.monthlyCost) },
            { header: '', render: (p) => <Button variant="ghost" onClick={() => enroll(p.id)}>Enroll</Button> },
          ]}
        />
      </Card>

      <Card title="My enrollments">
        <Table
          rows={mine}
          empty="You have no enrollments yet."
          columns={[
            { header: 'Plan', render: (e) => e.benefitPlanName },
            { header: 'Coverage from', render: (e) => e.coverageStartDate },
            { header: 'Status', render: (e) => <Badge tone={e.status === 0 ? 'good' : 'neutral'}>{EnrollmentStatusLabels[e.status]}</Badge> },
            { header: '', render: (e) => e.status === 0 ? <Button variant="danger" onClick={() => cancel(e.id)}>Cancel</Button> : null },
          ]}
        />
      </Card>
    </div>
  );
}

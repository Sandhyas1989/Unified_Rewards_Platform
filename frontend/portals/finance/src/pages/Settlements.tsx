import React, { useEffect, useState } from 'react';
import {
  api, Card, Button, Table, Banner, money, useUsers,
  ClaimTypeLabels, type ClaimDto,
} from '@urp/shared';

export function Settlements() {
  const [claims, setClaims] = useState<ClaimDto[]>([]);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const { byId } = useUsers();

  const load = async () => {
    setErr('');
    try { setClaims(await api.getItems<ClaimDto>('/claims?status=2&pageSize=100')); } // Approved
    catch (e: any) { setErr(e.message); }
  };
  useEffect(() => { load(); }, []);

  const settle = async (id: string) => {
    setErr(''); setMsg('');
    try {
      await api.post(`/claims/${id}/settle`);
      setMsg('Claim settled and pushed to payroll.');
      load();
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />
      <Card title="Approved claims awaiting settlement">
        <Table
          rows={claims}
          empty="No approved claims to settle."
          columns={[
            { header: 'Employee', render: (c) => byId(c.employeeId)?.fullName ?? c.employeeId.slice(0, 8) },
            { header: 'Type', render: (c) => ClaimTypeLabels[c.type] },
            { header: 'Amount', render: (c) => money(c.amount) },
            { header: '', render: (c) => <Button variant="primary" onClick={() => settle(c.id)}>Settle</Button> },
          ]}
        />
      </Card>
    </div>
  );
}

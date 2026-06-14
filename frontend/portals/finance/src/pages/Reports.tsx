import React, { useEffect, useState } from 'react';
import { api, saveBlob, Card, Button, Table, Banner, money } from '@urp/shared';

interface StatusAmountRow { status: string; count: number; totalAmount: number; }
interface DashboardDto {
  generatedAtUtc: string;
  claimsByStatus: StatusAmountRow[];
  settlementsByStatus: StatusAmountRow[];
  totalClaims: number;
  totalSettlements: number;
  note: string;
}

export function Reports() {
  const [data, setData] = useState<DashboardDto | null>(null);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');

  useEffect(() => {
    api.get<DashboardDto>('/reports/dashboard').then(setData).catch((e) => setErr(e.message));
  }, []);

  const exportClaims = async () => {
    setErr(''); setMsg('');
    try {
      const blob = await api.getBlob('/reports/claims/export');
      saveBlob(blob, `claims-report-${new Date().toISOString().slice(0, 10)}.xlsx`);
      setMsg('Claims report downloaded.');
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <div>
      <Banner kind="error" message={err} />
      <Banner kind="success" message={msg} />

      <Card title="Operational dashboard (aggregated across services)" actions={<Button onClick={exportClaims}>Export claims (.xlsx)</Button>}>
        {!data ? <p className="urp-empty">Loading…</p> : (
          <>
            <div className="urp-grid" style={{ marginBottom: 18 }}>
              <div className="urp-stat"><div className="urp-stat__label">Total claims</div><div className="urp-stat__value">{data.totalClaims}</div></div>
              <div className="urp-stat"><div className="urp-stat__label">Total settlements</div><div className="urp-stat__value">{data.totalSettlements}</div></div>
            </div>
            <h4>Claims by status</h4>
            <Table rows={data.claimsByStatus} empty="No claims." columns={[
              { header: 'Status', render: (r) => r.status },
              { header: 'Count', render: (r) => r.count },
              { header: 'Total', render: (r) => money(r.totalAmount) },
            ]} />
            <h4>Settlements by status</h4>
            <Table rows={data.settlementsByStatus} empty="No settlements." columns={[
              { header: 'Status', render: (r) => r.status },
              { header: 'Count', render: (r) => r.count },
              { header: 'Total', render: (r) => money(r.totalAmount) },
            ]} />
            <p className="urp-empty" style={{ marginTop: 12 }}>{data.note}</p>
          </>
        )}
      </Card>
    </div>
  );
}

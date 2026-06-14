import React, { useEffect, useState } from 'react';
import {
  api, Card, Table, Banner, money,
  CompensationStatusLabels, GradeBandLabels,
  type CompensationStructureDto,
} from '@urp/shared';

export function Compensation() {
  const [structures, setStructures] = useState<CompensationStructureDto[]>([]);
  const [err, setErr] = useState('');

  useEffect(() => {
    (async () => {
      try { setStructures(await api.get<CompensationStructureDto[]>('/compensation/me')); }
      catch (e: any) { setErr(e.message); }
    })();
  }, []);

  return (
    <div>
      <Banner kind="error" message={err} />
      {structures.length === 0 && <Card title="My compensation"><p className="urp-empty">No compensation structure on record.</p></Card>}
      {structures.map((s) => (
        <Card
          key={s.id}
          title={`${GradeBandLabels[s.grade]} · effective ${s.effectiveFrom} · ${CompensationStatusLabels[s.status]}`}
        >
          <div className="urp-grid" style={{ marginBottom: 16 }}>
            <div className="urp-stat"><div className="urp-stat__label">Gross (annual)</div><div className="urp-stat__value">{money(s.grossAnnual)}</div></div>
            <div className="urp-stat"><div className="urp-stat__label">Deductions</div><div className="urp-stat__value">{money(s.totalDeductions)}</div></div>
            <div className="urp-stat"><div className="urp-stat__label">Net (annual)</div><div className="urp-stat__value">{money(s.netAnnual)}</div></div>
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

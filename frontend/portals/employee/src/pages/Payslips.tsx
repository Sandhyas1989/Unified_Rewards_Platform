import React, { useEffect, useState } from 'react';
import { api, Card, Table, Banner, money, type PayslipDto } from '@urp/shared';

const MONTHS = ['', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

export function Payslips() {
  const [payslips, setPayslips] = useState<PayslipDto[]>([]);
  const [err, setErr] = useState('');

  useEffect(() => {
    (async () => {
      try { setPayslips(await api.getItems<PayslipDto>(`/payslips/me?pageSize=100`)); }
      catch (e: any) { setErr(e.message); }
    })();
  }, []);

  return (
    <div>
      <Banner kind="error" message={err} />
      <Card title="My payslips">
        <Table
          rows={payslips}
          empty="No payslips available yet."
          columns={[
            { header: 'Period', render: (p) => `${MONTHS[p.month]} ${p.year}` },
            { header: 'Gross', render: (p) => money(p.grossMonthly) },
            { header: 'Deductions', render: (p) => money(p.totalDeductionsMonthly) },
            { header: 'Net', render: (p) => <strong>{money(p.netMonthly)}</strong> },
          ]}
        />
      </Card>
    </div>
  );
}

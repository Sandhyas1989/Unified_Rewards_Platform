import React, { useState } from 'react';
import { Benefits } from './pages/Benefits';
import { Claims } from './pages/Claims';
import { Payslips } from './pages/Payslips';
import { Compensation } from './pages/Compensation';
import { Promotions } from './pages/Promotions';

const TABS = [
  { key: 'benefits', label: 'Benefits', render: () => <Benefits /> },
  { key: 'claims', label: 'Claims', render: () => <Claims /> },
  { key: 'payslips', label: 'Payslips', render: () => <Payslips /> },
  { key: 'compensation', label: 'My Compensation', render: () => <Compensation /> },
  { key: 'promotions', label: 'Bonus Campaigns', render: () => <Promotions /> },
];

export default function EmployeeApp() {
  const [active, setActive] = useState('benefits');
  const tab = TABS.find((t) => t.key === active)!;
  return (
    <div>
      <nav className="urp-tabs">
        {TABS.map((t) => (
          <button
            key={t.key}
            className={`urp-tab ${t.key === active ? 'urp-tab--active' : ''}`}
            onClick={() => setActive(t.key)}
          >
            {t.label}
          </button>
        ))}
      </nav>
      {tab.render()}
    </div>
  );
}

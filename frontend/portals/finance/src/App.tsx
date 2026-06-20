import React, { useState } from 'react';
import { Settlements } from './pages/Settlements';
import { Payroll } from './pages/Payroll';
import { Reports } from './pages/Reports';
import { Audit } from './pages/Audit';

const TABS = [
  { key: 'settlements', label: 'Claim Settlement', render: () => <Settlements /> },
  { key: 'payroll', label: 'Payroll', render: () => <Payroll /> },
  { key: 'reports', label: 'Reports', render: () => <Reports /> },
  { key: 'audit', label: 'Audit Trail', render: () => <Audit /> },
];

export default function FinanceApp() {
  const [active, setActive] = useState('settlements');
  const tab = TABS.find((t) => t.key === active)!;
  return (
    <div>
      <nav className="urp-tabs">
        {TABS.map((t) => (
          <button key={t.key} className={`urp-tab ${t.key === active ? 'urp-tab--active' : ''}`} onClick={() => setActive(t.key)}>
            {t.label}
          </button>
        ))}
      </nav>
      {tab.render()}
    </div>
  );
}

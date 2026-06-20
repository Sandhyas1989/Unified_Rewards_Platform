import React, { useState } from 'react';
import { Users } from './pages/Users';
import { BenefitPlans } from './pages/BenefitPlans';
import { Compensation } from './pages/Compensation';
import { Promotions } from './pages/Promotions';
import { Audit } from './pages/Audit';

const TABS = [
  { key: 'users', label: 'Users', render: () => <Users /> },
  { key: 'plans', label: 'Benefit Plans', render: () => <BenefitPlans /> },
  { key: 'comp', label: 'Compensation', render: () => <Compensation /> },
  { key: 'promotions', label: 'Bonus Campaigns', render: () => <Promotions /> },
  { key: 'audit', label: 'Audit Trail', render: () => <Audit /> },
];

export default function HrApp() {
  const [active, setActive] = useState('users');
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

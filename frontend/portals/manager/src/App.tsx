import React, { useState } from 'react';
import { ClaimsReview } from './pages/ClaimsReview';
import { Promotions } from './pages/Promotions';

const TABS = [
  { key: 'claims', label: 'Claims Review', render: () => <ClaimsReview /> },
  { key: 'promotions', label: 'Bonus Campaigns', render: () => <Promotions /> },
];

export default function ManagerApp() {
  const [active, setActive] = useState('claims');
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

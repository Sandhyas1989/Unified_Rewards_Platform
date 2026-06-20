import React, { useEffect, useState } from 'react';
import { api, Card, Table, Badge, Banner } from '@urp/shared';
import type { NominationDto, PromotionDto } from '@urp/shared';

const OUTCOME_LABELS = ['Pending', 'Awarded', 'Not Awarded', 'Withdrawn'];
const OUTCOME_TONE = ['warn', 'good', 'bad', 'neutral'] as const;

export function Promotions() {
  const [nominations, setNominations] = useState<NominationDto[]>([]);
  const [campaigns, setCampaigns] = useState<Map<string, PromotionDto>>(new Map());
  const [err, setErr] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const noms = await api.get<NominationDto[]>('/promotions/me/nominations');
        setNominations(noms);

        // Fetch campaign details for each unique promotion
        const ids = [...new Set(noms.map(n => n.promotionId))];
        const map = new Map<string, PromotionDto>();
        await Promise.all(ids.map(async id => {
          try {
            const p = await api.get<PromotionDto>(`/promotions/${id}`);
            map.set(id, p);
          } catch { /* skip if fetch fails */ }
        }));
        setCampaigns(map);
      } catch (e: any) { setErr(e.message); }
    };
    load();
  }, []);

  return (
    <div>
      <Banner kind="error" message={err} />
      <Card title="My Bonus Campaign Nominations">
        <Table
          rows={nominations}
          empty="You have not been nominated in any bonus campaign yet."
          columns={[
            { header: 'Campaign', render: n => campaigns.get(n.promotionId)?.title ?? n.promotionId.slice(0, 8) },
            { header: 'Cycle', render: n => { const p = campaigns.get(n.promotionId); return p ? `${p.cycleYear} ${p.cycleQuarter}` : '—'; } },
            { header: 'Bonus Value', render: n => { const p = campaigns.get(n.promotionId); return p ? `₹${p.bonusValue.toLocaleString()}` : '—'; } },
            { header: 'Nominated On', render: n => n.nominatedOn },
            { header: 'Outcome', render: n => <Badge tone={OUTCOME_TONE[n.outcome]}>{OUTCOME_LABELS[n.outcome]}</Badge> },
            { header: 'Remarks', render: n => n.remarks ?? '—' },
          ]}
        />
      </Card>
    </div>
  );
}

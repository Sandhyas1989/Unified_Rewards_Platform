import React from 'react';
import { Card, Banner } from '@urp/shared';

// No standalone Promotions microservice in the as-built backend (folded into Employee Profile /
// Compensation per the requirement). Shown as unavailable while wired to the microservices gateway.
export function Promotions() {
  return (
    <div>
      <Banner kind="error" message="Promotions is not available on the microservices backend." />
      <Card title="Promotion Committee">
        <p>
          The microservices architecture has no dedicated Promotions service — the requirement’s seven
          services fold this into the Employee Profile / Compensation domains. This screen is disabled
          while the portal targets the microservices gateway.
        </p>
      </Card>
    </div>
  );
}

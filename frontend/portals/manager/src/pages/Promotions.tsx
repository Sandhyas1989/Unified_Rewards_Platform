import React from 'react';
import { Card, Banner } from '@urp/shared';

// Promotions is NOT a standalone microservice in the as-built backend (the requirement's 7 services
// fold it into Employee Profile / Compensation). This portal targets the microservices gateway, so
// the capability is shown as unavailable rather than calling a non-existent endpoint.
export function Promotions() {
  return (
    <div>
      <Banner kind="error" message="Promotions is not available on the microservices backend." />
      <Card title="Promotion Nominations">
        <p>
          In the microservices architecture there is no dedicated Promotions service — per the requirement’s
          seven services, promotions are modelled as a capability of the Employee Profile / Compensation
          domains (and would be driven by domain events). This screen is intentionally disabled while the
          portal is wired to the microservices gateway.
        </p>
      </Card>
    </div>
  );
}

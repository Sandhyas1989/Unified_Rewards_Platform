import React from 'react';
import { Card, Banner } from '@urp/shared';

// The cross-cutting audit pipeline is a monolith feature. In the microservices architecture the
// Reporting & Compliance service is an event-sourced read model (fed by Service Bus events); a
// queryable audit endpoint is not part of the as-built local backend, so this is shown as unavailable.
export function Audit() {
  return (
    <div>
      <Banner kind="error" message="The audit trail endpoint is not available on the microservices backend." />
      <Card title="Audit Trail">
        <p>
          In the microservices architecture, auditing is the responsibility of the Reporting & Compliance
          service as an event-sourced read model fed by Azure Service Bus events. A queryable audit API is
          not part of the local as-built backend, so this screen is disabled while the portal targets the
          microservices gateway.
        </p>
      </Card>
    </div>
  );
}

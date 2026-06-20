import React, { Suspense } from 'react';
import { Banner } from '@urp/shared';
import { AuthProvider, useAuth } from './AuthContext';
import { Login } from './Login';
import { Layout } from './Layout';

// Each portal is code-split and loaded on demand based on the user's role. The portal
// sources are bundled directly into the shell (single-origin build for static hosting).
// Keys are the numeric UserRole values: Employee=0, Manager=1, HrAdmin=2, Finance=3.
const PORTALS: Record<number, React.LazyExoticComponent<React.ComponentType>> = {
  0: React.lazy(() => import('../../portals/employee/src/App')),
  1: React.lazy(() => import('../../portals/manager/src/App')),
  2: React.lazy(() => import('../../portals/hr/src/App')),
  3: React.lazy(() => import('../../portals/finance/src/App')),
};

class RemoteBoundary extends React.Component<{ children: React.ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  render() {
    if (this.state.failed) {
      return (
        <Banner
          kind="error"
          message="This portal could not be loaded. Make sure its remote dev server is running."
        />
      );
    }
    return this.props.children;
  }
}

function PortalHost() {
  const { user } = useAuth();
  const Portal = user ? PORTALS[user.role] : undefined;
  if (!Portal) {
    return <Banner kind="error" message={`No portal is configured for role "${user?.role}".`} />;
  }
  return (
    <RemoteBoundary>
      <Suspense fallback={<p>Loading portal…</p>}>
        <Portal />
      </Suspense>
    </RemoteBoundary>
  );
}

function Root() {
  const { user } = useAuth();
  if (!user) {
    return <Login />;
  }
  return (
    <Layout>
      <PortalHost />
    </Layout>
  );
}

export function App() {
  return (
    <AuthProvider>
      <Root />
    </AuthProvider>
  );
}

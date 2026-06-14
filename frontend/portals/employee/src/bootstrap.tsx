import React from 'react';
import { createRoot } from 'react-dom/client';
import '@urp/shared';
import App from './App';

// Standalone entry (running this remote on its own at :3001 for isolated development).
createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <div className="urp-shell__main">
      <App />
    </div>
  </React.StrictMode>,
);

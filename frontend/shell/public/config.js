// Runtime configuration — loaded before the app bundle.
// In Azure Container Apps: mount an overriding config.js via an init container or
// set GATEWAY_URL env var and have the nginx entrypoint rewrite this file at startup.
window.__URP_CONFIG__ = {
  apiBase: 'http://localhost:5080/api'
};

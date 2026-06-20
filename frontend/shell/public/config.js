// Runtime configuration — loaded before the app bundle.
// In Azure Container Apps: mount an overriding config.js via an init container or
// set GATEWAY_URL env var and have the nginx entrypoint rewrite this file at startup.
window.__URP_CONFIG__ = {
  apiBase: 'https://urp-gateway.thankfulwave-8e054ca3.centralindia.azurecontainerapps.io/api'
};

import http from 'k6/http';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const tenantId = __ENV.TENANT_ID;
const email = __ENV.ADMIN_EMAIL || 'admin@autonomuscrm.local';
const password = __ENV.ADMIN_PASSWORD || 'Admin123!';

let cachedToken = __ENV.AUTH_TOKEN || null;

export function authHeaders() {
  if (!cachedToken) {
    if (!tenantId) {
      return { Authorization: '' };
    }
    const payload = JSON.stringify({ email, password, tenantId });
    const res = http.post(`${baseUrl}/api/auth/login`, payload, {
      headers: { 'Content-Type': 'application/json' },
    });
    if (res.status === 200) {
      const body = JSON.parse(res.body);
      cachedToken = body.accessToken;
    }
  }
  return cachedToken
    ? { Authorization: `Bearer ${cachedToken}`, 'Content-Type': 'application/json' }
    : { 'Content-Type': 'application/json' };
}

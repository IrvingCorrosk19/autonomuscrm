import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  iterations: 3,
  thresholds: {
    http_req_failed: ['rate<0.05'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const tenantId = __ENV.TENANT_ID;
const email = __ENV.ADMIN_EMAIL || 'admin@autonomuscrm.local';
const password = __ENV.ADMIN_PASSWORD || 'Admin123!';

export default function () {
  if (!tenantId) {
    throw new Error('TENANT_ID env var required for login smoke');
  }

  const payload = JSON.stringify({
    email,
    password,
    tenantId,
  });

  const res = http.post(`${baseUrl}/api/auth/login`, payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'login status 200': (r) => r.status === 200,
    'login returns token': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.accessToken && body.accessToken.length > 20;
      } catch {
        return false;
      }
    },
  });
  sleep(1);
}

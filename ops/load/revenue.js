import http from 'k6/http';
import { check, sleep } from 'k6';
import { authHeaders } from './lib/auth.js';

export const options = {
  vus: 2,
  duration: '15s',
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<5000'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const tenantId = __ENV.TENANT_ID;

export default function () {
  if (!tenantId) throw new Error('TENANT_ID required');

  const headers = authHeaders();

  const endpoints = [
    `/api/revenue/dashboard?tenantId=${tenantId}`,
    `/api/revenue/os-dashboard?tenantId=${tenantId}`,
    `/api/revenue/kpis?tenantId=${tenantId}`,
  ];

  for (const path of endpoints) {
    const res = http.get(`${baseUrl}${path}`, { headers });
    check(res, {
      [`${path} not 500`]: (r) => r.status !== 500,
      [`${path} authorized or empty`]: (r) => r.status === 200 || r.status === 401,
    });
  }
  sleep(1);
}

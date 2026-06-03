import http from 'k6/http';
import { check, sleep } from 'k6';
import { authHeaders } from './lib/auth.js';

export const options = {
  vus: 2,
  duration: '15s',
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const tenantId = __ENV.TENANT_ID;

export default function () {
  if (!tenantId) throw new Error('TENANT_ID required');

  const headers = authHeaders();
  const paths = [
    `/api/memory?tenantId=${tenantId}`,
    `/api/ai/knowledge?tenantId=${tenantId}`,
  ];

  for (const path of paths) {
    const res = http.get(`${baseUrl}${path}`, { headers });
    check(res, {
      [`${path} not 500`]: (r) => r.status !== 500,
    });
  }
  sleep(1);
}

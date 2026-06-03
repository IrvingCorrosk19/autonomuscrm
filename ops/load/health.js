import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  duration: '10s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const res = http.get(`${baseUrl}/health`);
  check(res, {
    'health status 200': (r) => r.status === 200,
    'health body ok': (r) => r.body && r.body.includes('Healthy'),
  });
  sleep(1);
}

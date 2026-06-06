/**
 * RC Zero Gate 6 — responsive overflow detection (real browser)
 * Usage: node tests/e2e/run-responsive-gate.mjs
 */
import { chromium } from 'playwright';
import { writeFileSync, mkdirSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const base = process.env.CRM_BASE_URL || 'http://127.0.0.1:5154';
const email = process.env.CRM_ADMIN_EMAIL || 'admin@techsolutions.pa';
const password = process.env.CRM_ADMIN_PASSWORD || 'TechSolutions2026!';

const widths = [320, 375, 768, 1024, 1440, 1920, 2560];
const routes = [
  '/executive',
  '/revenue',
  '/Leads',
  '/Customers',
  '/Deals',
  '/Users',
  '/Workflows',
  '/Settings',
  '/Account/Login',
];

async function login(page) {
  await page.goto(`${base}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 30000 });
  const tenantField = page.locator('input[name="TenantId"]');
  if (await tenantField.count()) {
    await tenantField.fill('00000000-0000-0000-0000-000000000000');
  }
  await page.fill('input[name="Email"]', email);
  await page.fill('input[name="Password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL(u => !u.pathname.includes('/Account/Login'), { timeout: 20000 });
}

async function checkOverflow(page) {
  return page.evaluate(() => {
    const doc = document.documentElement;
    const body = document.body;
    const scrollW = Math.max(doc.scrollWidth, body?.scrollWidth || 0);
    const clientW = doc.clientWidth;
    const overflow = scrollW - clientW;
    const isExcluded = (el) => {
      if (!el) return true;
      if (el.closest('.table-responsive, .flow-drawer, .flow-palette-overlay, .flow-sidebar, [hidden], .crm-overlay-modal')) return true;
      if (el.closest('.flow-drawer:not(.is-open)')) return true;
      const style = getComputedStyle(el);
      if (style.visibility === 'hidden' || style.display === 'none') return true;
      return false;
    };
    const offScreen = [];
    for (const el of document.querySelectorAll('button, a.flow-btn, input, select, textarea')) {
      if (isExcluded(el)) continue;
      const r = el.getBoundingClientRect();
      if (r.width === 0 && r.height === 0) continue;
      if (r.right > clientW + 2 || r.left < -2) {
        const label = el.getAttribute('aria-label') || el.textContent?.trim()?.slice(0, 40) || el.tagName;
        offScreen.push(label);
      }
    }
    return { scrollW, clientW, overflow, offScreen: offScreen.slice(0, 5) };
  });
}

const results = [];

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    await login(page);
  } catch (e) {
    console.error('[FAIL] LOGIN', e.message);
    process.exit(1);
  }

  for (const route of routes) {
    for (const width of widths) {
      const id = `RSP-${route.replace(/\//g, '-').replace(/^-/, '') || 'home'}-${width}`;
      try {
        await page.setViewportSize({ width, height: 900 });
        await page.goto(`${base}${route}`, { waitUntil: 'networkidle', timeout: 30000 });
        const { overflow, offScreen, scrollW, clientW } = await checkOverflow(page);
        const has500 = (await page.content()).match(/Internal Server Error|NullReferenceException/);
        let status = 'PASS';
        let note = `scroll=${scrollW}/${clientW}`;
        if (has500) {
          status = 'FAIL';
          note = '500 in page';
        } else if (overflow > 8) {
          status = 'FAIL';
          note = `horizontal overflow ${overflow}px`;
        }
        results.push({ Id: id, Route: route, Width: width, Status: status, Note: note });
        const color = status === 'PASS' ? '\x1b[32m' : '\x1b[31m';
        console.log(`${color}[${status}]\x1b[0m ${id} — ${note}`);
      } catch (e) {
        results.push({ Id: id, Route: route, Width: width, Status: 'FAIL', Note: e.message });
        console.log(`\x1b[31m[FAIL]\x1b[0m ${id} — ${e.message}`);
      }
    }
  }

  await browser.close();

  const date = new Date().toISOString().slice(0, 10);
  const outDir = join(__dirname, '..', 'qa-evidence', 'responsive', date);
  mkdirSync(outDir, { recursive: true });
  const ts = new Date().toISOString().replace(/[:.]/g, '').slice(0, 15);
  const csv = ['Id,Route,Width,Status,Note', ...results.map(r =>
    `${r.Id},${r.Route},${r.Width},${r.Status},"${(r.Note || '').replace(/"/g, '""')}"`
  )].join('\n');
  const outFile = join(outDir, `responsive-${ts}.csv`);
  writeFileSync(outFile, csv, 'utf8');

  const fail = results.filter(r => r.Status === 'FAIL').length;
  const pass = results.filter(r => r.Status === 'PASS').length;
  console.log(`\nEvidence: ${outFile} | PASS=${pass} FAIL=${fail}`);
  process.exit(fail > 0 ? 1 : 0);
}

main();

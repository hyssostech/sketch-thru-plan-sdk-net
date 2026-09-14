#!/usr/bin/env node
/*
 * Controls for assert-coverage-collected.mjs (STP-740).
 *
 * The gate's whole claim is that it notices when a coverage run measured
 * nothing. A gate that has only ever been observed passing supports no such
 * claim - so each way the collector can silently fail gets a control here that
 * must be REJECTED, plus one well-formed report that must be accepted.
 *
 * The last of these is the one that matters most. `dotnet test --collect` with
 * a misspelled collector name, ignored runsettings, or an over-broad Exclude
 * exits 0 and produces no report at all; without the first control below, this
 * repository would go green on exactly that.
 *
 * Run: node .github/scripts/tests/test-assert-coverage-collected.mjs
 */

import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const GATE = join(HERE, '..', 'assert-coverage-collected.mjs');

let failures = 0;
const tmp = mkdtempSync(join(tmpdir(), 'covgate-'));

function cobertura({ valid, covered, packages }) {
  const pkgs = packages.map((p) => `<package name="${p}"/>`).join('');
  return (
    '<?xml version="1.0"?>\n' +
    `<coverage lines-valid="${valid}" lines-covered="${covered}">` +
    `<packages>${pkgs}</packages></coverage>\n`
  );
}

function scenario(name, build) {
  const dir = join(tmp, name);
  mkdirSync(dir, { recursive: true });
  build(dir);
  return dir;
}

function check(label, dir, expectPass, mustMention = []) {
  let out = '';
  let code = 0;
  try {
    out = execFileSync(process.execPath, [GATE, dir], { encoding: 'utf8' });
  } catch (e) {
    out = `${e.stdout || ''}${e.stderr || ''}`;
    code = e.status ?? 1;
  }
  const passed = code === 0;
  const ok = passed === expectPass;
  const missing = mustMention.filter((m) => !out.toLowerCase().includes(m.toLowerCase()));

  if (!ok || missing.length > 0) {
    failures += 1;
    console.log(`  WRONG  ${label}`);
    console.log(`         expected ${expectPass ? 'PASS' : 'FAIL'}, got ${passed ? 'PASS' : 'FAIL'} (exit ${code})`);
    for (const m of missing) console.log(`         expected the message to mention: ${m}`);
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log(`  OK     ${label}`);
  }
}

console.log('=== the collector silently produced nothing ===');
check(
  'an empty results directory must FAIL',
  scenario('empty', () => {}),
  false,
  ['no coverage.cobertura.xml', 'exited 0'],
);

console.log('');
console.log('=== the report exists but measured almost nothing ===');
check(
  'a near-empty report must FAIL, not read as an honest 0%',
  scenario('tiny', (d) => {
    writeFileSync(join(d, 'coverage.cobertura.xml'), cobertura({ valid: 12, covered: 3, packages: ['StpSDK'] }));
    writeFileSync(join(d, 'coverage.opencover.xml'), '<CoverageSession/>');
  }),
  false,
  ['measurable lines'],
);

console.log('');
console.log('=== the Exclude in .runsettings was dropped ===');
check(
  'the test assembly appearing in the report must FAIL',
  scenario('selftest', (d) => {
    writeFileSync(
      join(d, 'coverage.cobertura.xml'),
      cobertura({ valid: 5000, covered: 4900, packages: ['StpSDK', 'StpSDK.Tests'] }),
    );
    writeFileSync(join(d, 'coverage.opencover.xml'), '<CoverageSession/>');
  }),
  false,
  ['tests testing themselves'],
);

console.log('');
console.log('=== the collector attached to the wrong thing ===');
check(
  'a report with no product package must FAIL',
  scenario('wrongtarget', (d) => {
    writeFileSync(
      join(d, 'coverage.cobertura.xml'),
      cobertura({ valid: 5000, covered: 1600, packages: ['SomeOtherLibrary'] }),
    );
    writeFileSync(join(d, 'coverage.opencover.xml'), '<CoverageSession/>');
  }),
  false,
  ['not to the product'],
);

console.log('');
console.log('=== the OpenCover format was silently lost ===');
check(
  'Cobertura without OpenCover must FAIL (Sonar can read only the latter)',
  scenario('nooc', (d) => {
    writeFileSync(
      join(d, 'coverage.cobertura.xml'),
      cobertura({ valid: 5000, covered: 1600, packages: ['StpSDK'] }),
    );
  }),
  false,
  ['opencover'],
);

console.log('');
console.log('=== a malformed report is not coverage ===');
check(
  'a Cobertura file with no line totals must FAIL',
  scenario('noattrs', (d) => {
    writeFileSync(join(d, 'coverage.cobertura.xml'), '<?xml version="1.0"?>\n<coverage><packages><package name="StpSDK"/></packages></coverage>\n');
    writeFileSync(join(d, 'coverage.opencover.xml'), '<CoverageSession/>');
  }),
  false,
  ['lines-valid'],
);

console.log('');
console.log('=== and the counter-case: a good report is accepted ===');
check(
  'a well-formed report measuring the product must PASS',
  scenario('good', (d) => {
    const nested = join(d, 'guid-like-subdir');
    mkdirSync(nested, { recursive: true });
    writeFileSync(
      join(nested, 'coverage.cobertura.xml'),
      cobertura({ valid: 5128, covered: 1660, packages: ['StpSDK', 'JMSML'] }),
    );
    writeFileSync(join(nested, 'coverage.opencover.xml'), '<CoverageSession/>');
  }),
  true,
  ['PASS'],
);

rmSync(tmp, { recursive: true, force: true });

console.log('');
if (failures > 0) {
  console.log(`${failures} control(s) did not behave as required.`);
  process.exit(1);
}
console.log('All controls behaved as required: the coverage gate rejects every way the');
console.log('collector can silently measure nothing, and accepts a real report.');

#!/usr/bin/env node
/*
 * Controls for assert-sonar-pragmas-governed.mjs (STP-743 method, SDK repos).
 *
 * The checker's claim is that a Sonar-rule suppression cannot enter this
 * repository unjustified, undated, misattributed, or unbounded. A checker that
 * has only ever been observed passing supports no such claim - which is the
 * lesson the engine effort learned the hard way, from a quality gate that
 * returned {"status":"OK","conditions":[]} and stayed green for sixty runs
 * because it evaluated nothing.
 *
 * So each way a suppression can evade governance gets a control here that must
 * be REJECTED, plus two that must be ACCEPTED - a compliant Sonar pragma, and a
 * compiler-warning pragma that this checker deliberately does not govern.
 *
 * That second acceptance is not filler. If the checker governed CS#### and
 * CA#### too it would fire on nineteen pre-existing, entirely legitimate
 * pragmas in this repository, and the rule would be abandoned within a week for
 * noise. Scope is what makes it survivable, so scope is tested.
 *
 * Run: node .github/scripts/tests/test-assert-sonar-pragmas-governed.mjs
 */
import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const CHECKER = join(HERE, '..', 'assert-sonar-pragmas-governed.mjs');

// Fixed so the expiry control is deterministic. A test that only starts failing
// once a real date rolls past is a test that proves nothing on the day it is
// written, which is the only day anyone watches it.
const TODAY = '2026-09-14';

let failures = 0;
const tmp = mkdtempSync(join(tmpdir(), 'pragma-gov-'));

function scenario(name, body) {
  const dir = join(tmp, name);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, 'Thing.cs'), body);
  return dir;
}

function check(label, dir, expectPass, mustMention = []) {
  let out = '';
  let code = 0;
  try {
    out = execFileSync(process.execPath, [CHECKER, dir, TODAY], { encoding: 'utf8' });
  } catch (e) {
    out = `${e.stdout || ''}${e.stderr || ''}`;
    code = e.status ?? 1;
  }
  const passed = code === 0;
  const ok = passed === expectPass;
  const missing = mustMention.filter((m) => !out.toLowerCase().includes(m.toLowerCase()));

  if (!ok || missing.length) {
    failures += 1;
    console.log(`  WRONG  ${label}`);
    console.log(`         expected ${expectPass ? 'PASS' : 'FAIL'}, got ${passed ? 'PASS' : 'FAIL'} (exit ${code})`);
    for (const m of missing) console.log(`         expected the message to mention: ${m}`);
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log(`  OK     ${label}`);
  }
}

const GOOD = `namespace X;
// SONAR-DISPOSITION: S3875 value-like public type, removal is a silent break.
// REVIEW: 2027-03-14
#pragma warning disable S3875
public class Thing { }
#pragma warning restore S3875
`;

console.log('=== a suppression with no stated reason ===');
check(
  'missing SONAR-DISPOSITION must FAIL',
  scenario('nomarker', `namespace X;
// REVIEW: 2027-03-14
#pragma warning disable S3875
public class Thing { }
#pragma warning restore S3875
`),
  false,
  ['no "SONAR-DISPOSITION', 'deleted rather than answered'],
);

console.log('');
console.log('=== a suppression that never expires ===');
check(
  'missing REVIEW date must FAIL',
  scenario('nodate', `namespace X;
// SONAR-DISPOSITION: S3875 because reasons.
#pragma warning disable S3875
public class Thing { }
#pragma warning restore S3875
`),
  false,
  ['REVIEW: YYYY-MM-DD', 'outlive'],
);

console.log('');
console.log('=== a suppression whose review date has passed ===');
check(
  'expired REVIEW date must FAIL',
  scenario('expired', `namespace X;
// SONAR-DISPOSITION: S3875 because reasons.
// REVIEW: 2020-01-01
#pragma warning disable S3875
public class Thing { }
#pragma warning restore S3875
`),
  false,
  ['has passed', 're-argue'],
);

console.log('');
console.log('=== a justification belonging to a different rule ===');
check(
  'mismatched SONAR-DISPOSITION rule must FAIL',
  scenario('mismatch', `namespace X;
// SONAR-DISPOSITION: S1234 an argument about an entirely different rule.
// REVIEW: 2027-03-14
#pragma warning disable S3875
public class Thing { }
#pragma warning restore S3875
`),
  false,
  ['names S1234', 'misplaced'],
);

console.log('');
console.log('=== a suppression that runs to end of file ===');
check(
  'missing restore must FAIL',
  scenario('norestore', `namespace X;
// SONAR-DISPOSITION: S3875 because reasons.
// REVIEW: 2027-03-14
#pragma warning disable S3875
public class Thing { }
public class Unrelated { }
`),
  false,
  ['restore', 'nobody argued about'],
);

console.log('');
console.log('=== and the counter-cases, which matter just as much ===');
check('a compliant suppression must PASS', scenario('good', GOOD), true, ['PASS']);

check(
  'compiler-warning pragmas must be IGNORED, not governed',
  scenario('compilerwarn', `namespace X;
#pragma warning disable CS1591
#pragma warning disable CA1416
#pragma warning disable CS4014
public class Thing { }
`),
  true,
  ['Examined 0'],
);

rmSync(tmp, { recursive: true, force: true });

console.log('');
if (failures > 0) {
  console.log(`${failures} control(s) did not behave as required.`);
  process.exit(1);
}
console.log('All controls behaved as required: the governance check rejects every way a');
console.log('Sonar suppression can evade review, and leaves compiler pragmas alone.');

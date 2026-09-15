#!/usr/bin/env node
/*
 * Controls for assert-version-not-published.mjs.
 *
 * Every branch is exercised offline, against fixture index documents, because a
 * control that needs nuget.org to be reachable is a control that stops running
 * the day it is not - which is the same failure the checker exists to prevent.
 *
 * Run: node .github/scripts/tests/test-assert-version-not-published.mjs
 */
import { execFileSync } from 'node:child_process';
import { mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const CHECKER = join(HERE, '..', 'assert-version-not-published.mjs');

let failures = 0;
const tmp = mkdtempSync(join(tmpdir(), 'nugetdup-'));

function fixture(name, body) {
  const p = join(tmp, `${name}.json`);
  writeFileSync(p, typeof body === 'string' ? body : JSON.stringify(body));
  return p;
}

function check(label, args, expectPass, mustMention = []) {
  let out = '';
  let code = 0;
  try {
    out = execFileSync(process.execPath, [CHECKER, ...args], { encoding: 'utf8' });
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
    if (!ok) console.log(`         expected ${expectPass ? 'PASS' : 'FAIL'}, got ${passed ? 'PASS' : 'FAIL'} (exit ${code})`);
    for (const m of missing) console.log(`         expected the output to mention: ${m}`);
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log(`  OK     ${label}`);
  }
}

// The real nuget.org answer for this package, trimmed. Using the actual shape
// rather than an invented one, because the shape is the thing being parsed.
const REAL = fixture('real', {
  versions: ['0.4.1-preview', '0.4.2-preview', '0.5.0-rc.1', '0.5.0', '0.6.0-rc.1'],
});

console.log('=== the case this exists for: a tag re-cut against a stale <Version> ===');

check(
  'a version already on the registry must FAIL',
  ['HyssosTech.Sdk.STP', '0.5.0', '--index', REAL],
  false,
  ['ALREADY published', 'skip-duplicate', 'no delete, only unlist']
);

check(
  'the published candidate must FAIL too - a candidate is a real version',
  ['HyssosTech.Sdk.STP', '0.6.0-rc.1', '--index', REAL],
  false,
  ['ALREADY published']
);

check(
  'NuGet folds case on the pre-release label, so RC.1 must FAIL as well',
  ['HyssosTech.Sdk.STP', '0.6.0-RC.1', '--index', REAL],
  false,
  ['ALREADY published']
);

console.log('');
console.log('=== and the versions that are genuinely clear ===');

check(
  'an unpublished stable must PASS',
  ['HyssosTech.Sdk.STP', '0.6.0', '--index', REAL],
  true,
  ['not published yet']
);

check(
  'an unpublished candidate must PASS',
  ['HyssosTech.Sdk.STP', '0.6.0-rc.2', '--index', REAL],
  true,
  ['not published yet']
);

console.log('');
console.log('=== fail closed: an answer it cannot act on is not a green light ===');

check(
  'a malformed document must FAIL, not be read as "no versions"',
  ['HyssosTech.Sdk.STP', '0.6.0', '--index', fixture('garbage', '{"totally":"different"}')],
  false,
  ['did not answer', 'not publishing']
);

check(
  'unparseable JSON must FAIL',
  ['HyssosTech.Sdk.STP', '0.6.0', '--index', fixture('broken', '{not json at all')],
  false,
  ['could not read', 'not publishing']
);

check(
  'a missing index file must FAIL',
  ['HyssosTech.Sdk.STP', '0.6.0', '--index', join(tmp, 'does-not-exist.json')],
  false,
  ['could not read', 'not publishing']
);

check(
  'an EMPTY version list for a package that has releases must FAIL, not wave everything through',
  ['HyssosTech.Sdk.STP', '0.6.0', '--index', fixture('empty', { versions: [] })],
  false,
  ['empty version list']
);

console.log('');
console.log('=== missing arguments are a usage error, not a pass ===');
{
  let code = 0;
  try {
    execFileSync(process.execPath, [CHECKER], { encoding: 'utf8' });
  } catch (e) {
    code = e.status ?? 1;
  }
  if (code === 0) {
    failures += 1;
    console.log('  WRONG  running with no arguments exited 0');
  } else {
    console.log(`  OK     running with no arguments exits ${code}`);
  }
}

rmSync(tmp, { recursive: true, force: true });

console.log('');
if (failures > 0) {
  console.log(`${failures} control(s) did not behave as required.`);
  process.exit(1);
}
console.log('All controls behaved as required: a published version is rejected, case folding');
console.log('is handled, and every unusable answer fails closed rather than green.');

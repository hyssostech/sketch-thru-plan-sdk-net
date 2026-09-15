#!/usr/bin/env node
/*
 * Controls for assert-version-documents-agree.mjs.
 *
 * The checker's claim is that the version and the two documents describing it
 * cannot silently disagree. A checker only ever observed passing supports no
 * such claim - and in this repository two earlier "gates" turned out to be
 * measuring nothing while staying green (coverlet collecting no coverage; a
 * TFM-encoded path whose `-f` guard went false). So each control below asserts
 * on the MESSAGE and on a non-zero examined count, not merely on exit status.
 *
 * Run: node .github/scripts/tests/test-assert-version-documents-agree.mjs
 */
import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const CHECKER = join(HERE, '..', 'assert-version-documents-agree.mjs');

let failures = 0;
const tmp = mkdtempSync(join(tmpdir(), 'versiondocs-'));

const CSPROJ = (v) => `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
  </PropertyGroup>
  <PropertyGroup>
    <PackageId>HyssosTech.Sdk.STP</PackageId>
    <Version>${v}</Version>
  </PropertyGroup>
</Project>
`;

const CHANGELOG = (h) => `# Change Log

Some preamble that is not a heading.

${h}
- did a thing

## 0.4.2-preview
- did an older thing
`;

const NOTES = (h) => `# Release Notes

Preamble.

${h}

### Summary

Words.

## Version 0.4.2-preview
`;

/**
 * Build a fixture repository. Anything passed as null is omitted entirely.
 */
function repo(name, { version, changelog, notes, versionMd = null, extraProps = null }) {
  const dir = join(tmp, name);
  mkdirSync(join(dir, 'src', 'StpSDK.JsonRpc', 'Docs'), { recursive: true });
  if (version !== null) {
    writeFileSync(join(dir, 'src', 'StpSDK.JsonRpc', 'StpSDK.csproj'), CSPROJ(version));
  } else {
    // A project file with no <Version> at all, so the walk still finds files.
    writeFileSync(
      join(dir, 'src', 'StpSDK.JsonRpc', 'StpSDK.csproj'),
      '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup/></Project>\n'
    );
  }
  if (changelog !== null) writeFileSync(join(dir, 'CHANGELOG.md'), CHANGELOG(changelog));
  if (notes !== null) {
    writeFileSync(join(dir, 'src', 'StpSDK.JsonRpc', 'Docs', 'ReleaseNotes.md'), NOTES(notes));
  }
  if (versionMd !== null) writeFileSync(join(dir, 'VERSION.md'), versionMd);
  if (extraProps !== null) writeFileSync(join(dir, 'Directory.Build.props'), extraProps);
  return dir;
}

function check(label, dir, expectPass, mustMention = []) {
  let out = '';
  let code = 0;
  try {
    out = execFileSync(process.execPath, [CHECKER, dir], { encoding: 'utf8' });
  } catch (e) {
    out = `${e.stdout || ''}${e.stderr || ''}`;
    code = e.status ?? 1;
  }
  const passed = code === 0;
  const ok = passed === expectPass;

  // A run that walked no project files proves nothing, whichever way it exited.
  const examined = /Examined (\d+) project/.exec(out);
  const measured = examined && Number(examined[1]) > 0;

  const missing = mustMention.filter((m) => !out.toLowerCase().includes(m.toLowerCase()));

  if (!ok || !measured || missing.length) {
    failures += 1;
    console.log(`  WRONG  ${label}`);
    if (!ok) {
      console.log(`         expected ${expectPass ? 'PASS' : 'FAIL'}, got ${passed ? 'PASS' : 'FAIL'} (exit ${code})`);
    }
    if (!measured) console.log('         examined ZERO project files - it measured nothing');
    for (const m of missing) console.log(`         expected the output to mention: ${m}`);
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log(`  OK     ${label}`);
  }
}

console.log('=== the drift this exists to catch ===');

check(
  'CHANGELOG a release behind the package must FAIL',
  repo('changelog-behind', { version: '0.6.0-rc.1', changelog: '## 0.5.0', notes: '## Version 0.6.0' }),
  false,
  ['CHANGELOG.md', 'says 0.5.0', '0.6.0-rc.1']
);

check(
  'ReleaseNotes a release behind the package must FAIL',
  repo('notes-behind', { version: '0.6.0-rc.1', changelog: '## 0.6.0', notes: '## Version 0.5.0' }),
  false,
  ['ReleaseNotes.md', 'says 0.5.0']
);

check(
  'the exact live state of main before this ticket must FAIL',
  repo('live-drift', {
    version: '0.6.0-rc.1',
    changelog: '## 0.5.0',
    notes: '## Version 0.5.0',
    versionMd: '0.5.0\n',
  }),
  false,
  ['CHANGELOG.md', 'ReleaseNotes.md', 'VERSION.md']
);

console.log('');
console.log('=== the placeholder that made the drift invisible ===');

check(
  '"## Unreleased" must FAIL, not pass by being un-parseable',
  repo('unreleased', { version: '0.6.0', changelog: '## Unreleased', notes: '## Version 0.6.0' }),
  false,
  ['placeholder', 'Unreleased']
);

console.log('');
console.log('=== a single version source ===');

check(
  'a second <Version> anywhere must FAIL',
  repo('two-versions', {
    version: '0.6.0',
    changelog: '## 0.6.0',
    notes: '## Version 0.6.0',
    extraProps: '<Project><PropertyGroup><Version>0.5.0</Version></PropertyGroup></Project>\n',
  }),
  false,
  ['more than one <Version>', 'Directory.Build.props']
);

check(
  'no <Version> at all must FAIL rather than quietly pass',
  repo('no-version', { version: null, changelog: '## 0.6.0', notes: '## Version 0.6.0' }),
  false,
  ['No <Version> element', '1.0.0']
);

check(
  'VERSION.md coming back must FAIL',
  repo('version-md', {
    version: '0.6.0',
    changelog: '## 0.6.0',
    notes: '## Version 0.6.0',
    versionMd: '0.6.0\n',
  }),
  false,
  ['VERSION.md']
);

console.log('');
console.log('=== a missing document is not a pass ===');

check(
  'a deleted CHANGELOG must FAIL',
  repo('no-changelog', { version: '0.6.0', changelog: null, notes: '## Version 0.6.0' }),
  false,
  ['CHANGELOG.md is missing']
);

check(
  'deleted release notes must FAIL - they ship inside the package',
  repo('no-notes', { version: '0.6.0', changelog: '## 0.6.0', notes: null }),
  false,
  ['ReleaseNotes.md is missing']
);

console.log('');
console.log('=== and the counter-cases, where a blunt rule would die ===');

check(
  'all three in exact agreement must PASS',
  repo('exact', { version: '0.6.0', changelog: '## 0.6.0', notes: '## Version 0.6.0' }),
  true,
  ['PASS']
);

check(
  'a candidate documented under its release base must PASS (how 0.5.0 was written)',
  repo('rc-base', { version: '0.6.0-rc.1', changelog: '## 0.6.0', notes: '## Version 0.6.0' }),
  true,
  ['PASS']
);

check(
  'a candidate documented under its own exact string must PASS too',
  repo('rc-exact', { version: '0.6.0-rc.1', changelog: '## 0.6.0-rc.1', notes: '## Version 0.6.0-rc.1' }),
  true,
  ['PASS']
);

check(
  'the base must not match a DIFFERENT release - 0.7.0 docs on a 0.6.0 package must FAIL',
  repo('ahead', { version: '0.6.0-rc.1', changelog: '## 0.7.0', notes: '## Version 0.7.0' }),
  false,
  ['says 0.7.0']
);

console.log('');
console.log('=== the check must not report health when it examined nothing ===');
{
  const empty = join(tmp, 'empty-repo');
  mkdirSync(empty, { recursive: true });
  let out = '';
  let code = 0;
  try {
    out = execFileSync(process.execPath, [CHECKER, empty], { encoding: 'utf8' });
  } catch (e) {
    out = `${e.stdout || ''}${e.stderr || ''}`;
    code = e.status ?? 1;
  }
  if (code === 0 || !/measured NOTHING/i.test(out)) {
    failures += 1;
    console.log('  WRONG  a tree with no project files must fail loudly, not pass');
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log('  OK     a tree with no project files fails loudly, not silently');
  }
}

rmSync(tmp, { recursive: true, force: true });

console.log('');
if (failures > 0) {
  console.log(`${failures} control(s) did not behave as required.`);
  process.exit(1);
}
console.log('All controls behaved as required: drift in either document fails, a placeholder');
console.log('heading fails, a second version source fails, and the candidate-under-its-base');
console.log('convention still passes.');

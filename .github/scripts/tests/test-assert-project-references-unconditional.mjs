#!/usr/bin/env node
/*
 * Controls for assert-project-references-unconditional.mjs.
 *
 * The regression it guards against is real and specific - PR #13 - so the first
 * control reconstructs that exact csproj shape. The rest exist because the
 * obvious implementations of this check are wrong in ways a green run would
 * never reveal: a grep misses a condition on the element, a naive block tracker
 * leaks a condition past </ItemGroup> and flags innocent references, and a
 * check pointed at the wrong directory reports perfect health.
 *
 * Run: node .github/scripts/tests/test-assert-project-references-unconditional.mjs
 */
import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const CHECKER = join(HERE, '..', 'assert-project-references-unconditional.mjs');

let failures = 0;
const tmp = mkdtempSync(join(tmpdir(), 'projrefs-'));

function scenario(name, csproj) {
  const dir = join(tmp, name);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, 'Sample.csproj'), csproj);
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

  const checked = /Checked (\d+) <ProjectReference>/.exec(out);
  const measured = checked && Number(checked[1]) > 0;
  const missing = mustMention.filter((m) => !out.toLowerCase().includes(m.toLowerCase()));

  if (!ok || !measured || missing.length) {
    failures += 1;
    console.log(`  WRONG  ${label}`);
    if (!ok) console.log(`         expected ${expectPass ? 'PASS' : 'FAIL'}, got ${passed ? 'PASS' : 'FAIL'} (exit ${code})`);
    if (!measured) console.log('         checked ZERO project references - it measured nothing');
    for (const m of missing) console.log(`         expected the output to mention: ${m}`);
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log(`  OK     ${label}`);
  }
}

console.log('=== the regression this exists for: PR #13, reconstructed ===');

check(
  'a Debug-only ItemGroup around the SDK reference must FAIL',
  scenario('debug-only', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" />
  </ItemGroup>
</Project>
`),
  false,
  ['conditioned on', 'PR #13']
);

check(
  'the same defect the other way round - Release-only - must FAIL too',
  scenario('release-only', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup Condition="'$(Configuration)'=='Release'">
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" />
  </ItemGroup>
</Project>
`),
  false,
  ['conditioned on']
);

console.log('');
console.log('=== what the ticket\'s suggested grep for "ItemGroup Condition" would MISS ===');

check(
  'a Condition on the ProjectReference element itself must FAIL',
  scenario('element-cond', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" Condition="'$(Configuration)'=='Debug'" />
  </ItemGroup>
</Project>
`),
  false,
  ['carries Condition']
);

console.log('');
console.log('=== and the counter-cases, where a blunt rule would produce false reds ===');

check(
  'an unconditional reference must PASS',
  scenario('clean', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" />
  </ItemGroup>
</Project>
`),
  true,
  ['PASS']
);

check(
  'a conditioned PackageReference is ordinary and must PASS',
  scenario('pkg-cond', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup Condition="'$(TargetFramework)'=='netstandard2.0'">
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" />
  </ItemGroup>
</Project>
`),
  true,
  ['PASS']
);

check(
  'a condition must not leak past </ItemGroup> onto the next group',
  scenario('no-leak', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup Condition="'$(Configuration)'=='Debug'">
    <Compile Include="DebugOnly.cs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" />
  </ItemGroup>
</Project>
`),
  true,
  ['PASS']
);

check(
  'a self-closing <ItemGroup ... /> opens no block',
  scenario('self-closing', `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup Condition="'$(Configuration)'=='Debug'" />
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\StpSDK.JsonRpc\\StpSDK.csproj" />
  </ItemGroup>
</Project>
`),
  true,
  ['PASS']
);

console.log('');
console.log('=== a check pointed at nothing must not report health ===');
{
  const empty = join(tmp, 'no-projects');
  mkdirSync(empty, { recursive: true });
  let out = '';
  let code = 0;
  try {
    out = execFileSync(process.execPath, [CHECKER, empty], { encoding: 'utf8' });
  } catch (e) {
    out = `${e.stdout || ''}${e.stderr || ''}`;
    code = e.status ?? 1;
  }
  if (code === 0 || !/measured nothing/i.test(out)) {
    failures += 1;
    console.log('  WRONG  a directory with no project references must fail loudly');
    console.log(out.split('\n').map((l) => `         ${l}`).join('\n'));
  } else {
    console.log('  OK     a directory with no project references fails loudly, not silently');
  }
}

rmSync(tmp, { recursive: true, force: true });

console.log('');
if (failures > 0) {
  console.log(`${failures} control(s) did not behave as required.`);
  process.exit(1);
}
console.log('All controls behaved as required: both conditioned shapes fail, conditioned');
console.log('package references and neighbouring conditioned groups still pass, and a check');
console.log('that examined nothing says so.');

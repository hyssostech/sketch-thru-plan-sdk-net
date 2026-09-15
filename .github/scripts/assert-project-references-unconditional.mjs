#!/usr/bin/env node
/*
 * STP-699 Phase 4. A sample that builds in Debug and is impossible in Release.
 *
 * PR #13 fixed exactly that: the StpSDK <ProjectReference> in the sample and
 * plugin projects sat inside an ItemGroup conditioned on the Debug
 * configuration, so no sample could be built, packed or shipped in Release. It
 * was invisible for as long as nothing built the samples at all.
 *
 * They are built now - samples, quickstart and plugins joined StpSDK.sln, and
 * build-test.yml builds that solution in Release. This check is the other half:
 * a Release build proves the samples work TODAY, and this proves the specific
 * defect cannot come back in a form the build would not notice. A reference
 * conditioned the other way round - present in Release, absent in Debug - would
 * pass a Release-only CI and break every developer's F5.
 *
 * The ticket asks for "a grep for `ItemGroup Condition`". That is narrower than
 * the defect: the condition can sit on the ProjectReference element itself, and
 * a grep would not see it. This checks both.
 *
 * It does NOT object to conditions in general. Conditioned PackageReferences
 * are ordinary and sometimes necessary - a TFM-specific dependency, for
 * instance. Only ProjectReference is in scope, because a project reference that
 * appears and disappears is the thing that made a sample unbuildable.
 *
 * Usage: node assert-project-references-unconditional.mjs [root...]
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const roots = process.argv.slice(2);
if (roots.length === 0) roots.push('.');

const SKIP_DIRS = new Set(['.git', 'bin', 'obj', 'node_modules', 'artifacts', 'packages']);

function walk(dir, out = []) {
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return out;
  }
  for (const entry of entries) {
    const full = join(dir, entry);
    let st;
    try {
      st = statSync(full);
    } catch {
      continue;
    }
    if (st.isDirectory()) {
      if (!SKIP_DIRS.has(entry)) walk(full, out);
    } else if (entry.endsWith('.csproj')) {
      out.push(full);
    }
  }
  return out;
}

const files = [];
for (const root of roots) files.push(...walk(root));

const problems = [];
let referencesChecked = 0;

for (const file of files) {
  const lines = readFileSync(file, 'utf8').split(/\r?\n/);
  const shown = relative('.', file).split(sep).join('/');

  // Line-based rather than an XML parse: msbuild files here are hand-written
  // and flat, and a parser dependency in a CI gate is a cost of its own. The
  // shapes that matter are `<ItemGroup Condition=...>` opening a block and
  // `<ProjectReference ... Condition=...>` on the element.
  let groupCondition = null;
  let groupLine = 0;

  lines.forEach((line, i) => {
    const open = /<ItemGroup\b([^>]*)>/.exec(line);
    if (open) {
      const cond = /Condition\s*=\s*"([^"]*)"/.exec(open[1]);
      groupCondition = cond ? cond[1] : null;
      groupLine = i + 1;
      // A self-closing <ItemGroup ... /> opens nothing.
      if (/\/>\s*$/.test(open[0])) groupCondition = null;
    }
    if (/<\/ItemGroup>/.test(line)) groupCondition = null;

    const ref = /<ProjectReference\b([^>]*)>/.exec(line);
    if (!ref) return;
    referencesChecked += 1;

    const own = /Condition\s*=\s*"([^"]*)"/.exec(ref[1]);
    if (own) {
      problems.push(
        `${shown}:${i + 1}: <ProjectReference> carries Condition="${own[1]}".\n` +
          '        A project reference that appears in one configuration and not another\n' +
          '        is how a sample became buildable in Debug and impossible in Release.'
      );
    } else if (groupCondition !== null) {
      problems.push(
        `${shown}:${i + 1}: <ProjectReference> is inside an <ItemGroup> conditioned on\n` +
          `        "${groupCondition}" (opened at line ${groupLine}).\n` +
          '        This is the exact shape PR #13 removed: the reference vanished outside\n' +
          '        that configuration, so no sample could be built or packed in Release.'
      );
    }
  });
}

// A run that found no project references examined nothing. Whichever way it
// would have exited, it is not evidence, and this repository has already
// shipped two gates that were green while measuring nothing.
if (referencesChecked === 0) {
  console.error(
    `Found ${files.length} .csproj file(s) under ${roots.join(', ')} but NOT ONE ` +
      '<ProjectReference>. This check measured nothing - the paths are probably wrong.'
  );
  process.exit(1);
}

console.log(
  `Checked ${referencesChecked} <ProjectReference> across ${files.length} project(s) ` +
    `under ${roots.join(', ')}.`
);

if (problems.length > 0) {
  console.error('');
  console.error('Configuration-conditioned project references:');
  for (const p of problems) console.error(`  - ${p}`);
  process.exit(1);
}

console.log('PASS: every project reference is unconditional.');

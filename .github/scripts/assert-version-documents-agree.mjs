#!/usr/bin/env node
/*
 * STP-699 Phase 2. One version source, and documents that cannot fall behind it.
 *
 * The package version is read by `dotnet pack` from ONE place - <Version> in
 * src/StpSDK.JsonRpc/StpSDK.csproj. Three other things are supposed to agree
 * with it and none of them was checked:
 *
 *   - CHANGELOG.md's top heading
 *   - src/StpSDK.JsonRpc/Docs/ReleaseNotes.md's top heading, which is embedded
 *     into the shipped package via <PackageReleaseNotes>
 *   - VERSION.md, a second hand-maintained copy of the string, referenced by
 *     nothing
 *
 * This is not a hypothetical. When this check was written, 0.6.0-rc.1 was
 * already ON nuget.org while all three documents still said 0.5.0 - so the
 * release notes inside the published package described the previous release.
 *
 * WHAT IT ENFORCES
 *
 *   1. Exactly one <Version> element across every csproj/props file. Two is how
 *      "the single source" silently becomes two sources.
 *   2. No VERSION.md. Deleted rather than generated: a generated file still has
 *      to be regenerated, and nothing consumed it.
 *   3. The top version heading of each document names either the exact version
 *      or its release base (0.6.0-rc.1 -> 0.6.0 is accepted, which is how the
 *      0.5.0 notes were already written - one section covering the candidate
 *      and the release it is a candidate FOR).
 *
 * WHAT IT DELIBERATELY DOES NOT ACCEPT
 *
 * `## Unreleased`. That heading is how the drift happened: it is always
 * correct, so it never forces anyone to look. Naming the section after the
 * version being prepared is the whole mechanism.
 *
 * Usage: node .github/scripts/assert-version-documents-agree.mjs [repoRoot]
 */
import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const root = process.argv[2] ?? '.';

const CSPROJ_VERSION = /<Version>([^<]*)<\/Version>/g;
const SKIP_DIRS = new Set(['.git', 'bin', 'obj', 'node_modules', 'artifacts', 'packages']);

const problems = [];

function walk(dir) {
  const out = [];
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
      if (!SKIP_DIRS.has(entry)) out.push(...walk(full));
    } else if (entry.endsWith('.csproj') || entry.endsWith('.props')) {
      out.push(full);
    }
  }
  return out;
}

const rel = (f) => relative(root, f).split(sep).join('/');

/* ------------------------------------------------------------------ 1. source */

const projectFiles = walk(root);
const versionSites = [];

for (const file of projectFiles) {
  const text = readFileSync(file, 'utf8');
  const lines = text.split(/\r?\n/);
  lines.forEach((line, i) => {
    CSPROJ_VERSION.lastIndex = 0;
    const m = CSPROJ_VERSION.exec(line);
    if (m) versionSites.push({ file: rel(file), line: i + 1, value: m[1].trim() });
  });
}

// A run that found no project files at all examined nothing, whichever way it
// would have exited. Say so rather than reporting a clean bill of health.
if (projectFiles.length === 0) {
  console.error(`No .csproj or .props file found under ${root} - this check measured NOTHING.`);
  process.exit(1);
}

if (versionSites.length === 0) {
  problems.push(
    'No <Version> element anywhere. `dotnet pack` would fall back to 1.0.0 and publish it.'
  );
} else if (versionSites.length > 1) {
  problems.push(
    'More than one <Version> element - there is no single version source:\n' +
      versionSites.map((s) => `        ${s.file}:${s.line} -> ${s.value}`).join('\n')
  );
}

const version = versionSites.length === 1 ? versionSites[0].value : null;

/* -------------------------------------------------- 2. no second hand-copy */

if (existsSync(join(root, 'VERSION.md'))) {
  problems.push(
    'VERSION.md is back. It is a second hand-maintained copy of a string that\n' +
      '        already lives in the csproj, nothing reads it, and it drifted the last\n' +
      '        time it existed. The csproj is the source.'
  );
}

/* ---------------------------------------------------------- 3. the documents */

// Everything up to the first pre-release or build-metadata separator.
const releaseBase = (v) => v.split(/[-+]/)[0];

// `^##` and not `^###`: the release notes use `###` for Summary/Notes
// subsections, and matching those would make "the top heading" mean something
// different in the two documents.
const TOP_HEADING = /^##(?!#)\s+(.*?)\s*$/;

const DOCS = [
  {
    path: 'CHANGELOG.md',
    // "## 0.6.0", tolerating a trailing note such as "## 0.3.9 and earlier".
    version: /^(\d[^\s]*)/,
    example: '## 0.6.0',
  },
  {
    path: 'src/StpSDK.JsonRpc/Docs/ReleaseNotes.md',
    version: /^Version\s+(\d[^\s]*)/,
    example: '## Version 0.6.0',
  },
];

const found = [];

for (const doc of DOCS) {
  const full = join(root, doc.path);
  if (!existsSync(full)) {
    problems.push(`${doc.path} is missing - it is part of the release contract.`);
    continue;
  }
  const lines = readFileSync(full, 'utf8').split(/\r?\n/);

  // The TOP heading, not the first heading that happens to parse as a version.
  // Reading the first PARSEABLE one is what let `## Unreleased` sit above
  // `## 0.4.2-preview` and get reported as "says 0.4.2-preview" - a true
  // statement about the wrong line, and the placeholder branch below could
  // never run.
  let topLine = 0;
  let topText = null;
  for (let i = 0; i < lines.length; i += 1) {
    const m = TOP_HEADING.exec(lines[i]);
    if (m) {
      topText = m[1];
      topLine = i + 1;
      break;
    }
  }

  if (topText === null) {
    problems.push(`${doc.path}: no "##" section heading at all (expected "${doc.example}").`);
    continue;
  }

  const parsed = doc.version.exec(topText);
  if (!parsed) {
    problems.push(
      `${doc.path}:${topLine}: the top section is "${topText}", which is not a version.\n` +
        '        A placeholder such as "Unreleased" is always correct, which is exactly\n' +
        '        why it never forces anyone to look - that is how the drift this check\n' +
        `        exists for happened. Name the section "${doc.example}".`
    );
    continue;
  }

  const top = parsed[1];
  found.push({ path: doc.path, line: topLine, value: top });

  if (version && top !== version && top !== releaseBase(version)) {
    const accepted = [...new Set([version, releaseBase(version)])].map((v) => `"${v}"`).join(' or ');
    problems.push(
      `${doc.path}:${topLine}: says ${top}, but the package version is ${version}.\n` +
        `        Accepted headings: ${accepted}.`
    );
  }
}

/* --------------------------------------------------------------- report */

console.log(`Examined ${projectFiles.length} project/props file(s) under ${root}.`);
if (version) console.log(`  package version          ${version}  (${versionSites[0].file}:${versionSites[0].line})`);
for (const f of found) console.log(`  ${f.path.padEnd(24)} ${f.value}  (line ${f.line})`);

if (problems.length > 0) {
  console.error('');
  console.error('The version and the documents that describe it disagree:');
  for (const p of problems) console.error(`  - ${p}`);
  console.error('');
  console.error('The csproj <Version> is what nuget.org receives. A document that lags it');
  console.error('ships inside the package: <PackageReleaseNotes> embeds ReleaseNotes.md.');
  process.exit(1);
}

console.log('');
console.log('PASS: one version source, and both documents name it.');

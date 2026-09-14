/**
 * Prove a coverage run actually measured the product.
 *
 * WHY THIS EXISTS
 *
 * `dotnet test --collect:"XPlat Code Coverage"` exits 0 whether or not the
 * collector produced anything. It exits 0 when the data collector's friendly
 * name is misspelled, when the runsettings file is ignored, and when an
 * over-broad Exclude filter removes every assembly. In all three cases the
 * tests pass, the step is green, and the coverage report is absent or empty.
 *
 * That is the same failure this repository already lived through in another
 * shape: coverlet.collector sat in the test project's PackageReference list
 * long before any workflow asked for coverage, so the package was restored,
 * resolved, and carried in the dependency graph having measured nothing at
 * all. Checking the exit code would not have caught it then, and would not
 * catch it now.
 *
 * WHAT THIS CHECKS - AND WHAT IT DELIBERATELY DOES NOT
 *
 * This is a MEASUREMENT-INTEGRITY gate, not a quality ratchet. It asserts the
 * instrument was pointed at the subject:
 *
 *   - a Cobertura report exists and has the attributes it must have;
 *   - it accounts for a plausible number of lines, so an empty report fails
 *     rather than being displayed as an honest "0% coverage";
 *   - the product assemblies appear in it;
 *   - the TEST assembly does not, which is what proves the Exclude in
 *     .runsettings was honoured rather than silently dropped;
 *   - an OpenCover report exists too, because that is the only format
 *     SonarQube's C# analyser imports and its silent loss would disconnect
 *     coverage from analysis with no other symptom.
 *
 * It does NOT assert a coverage percentage. Coverage is ~32% today; whether
 * that is good enough is a different conversation from whether the number is
 * real. A threshold here would couple the two, and the first time the
 * threshold hurt, the honest half would be deleted along with it.
 *
 * Written in Node rather than Python because this job runs on both
 * ubuntu-latest and windows-latest, and node is already invoked on both legs
 * by the action-pin check. That is one less thing to be portable.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

// A floor, not a target. The product is ~5,000 measurable lines, so anything
// in the low hundreds means the collector attached to the wrong thing. Set far
// below the real figure so ordinary churn never trips it: this fires on a
// broken instrument, not on a shrinking codebase.
const MIN_LINES_VALID = 1000;

// Substring match, so StpSDK.JsonRpc and friends count.
const EXPECT_PACKAGES = ['StpSDK'];
// Exact match. If this appears, the tests are measuring themselves and every
// figure in the report is inflated.
const FORBID_PACKAGES = ['StpSDK.Tests'];

const root = process.argv[2];
if (!root) {
  console.error('usage: assert-coverage-collected.mjs <results-directory>');
  process.exit(2);
}

const fail = (msg) => {
  console.log(`FAIL: ${msg}`);
  process.exit(1);
};

function walk(dir) {
  let out = [];
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return out;
  }
  for (const e of entries) {
    const p = join(dir, e);
    let s;
    try {
      s = statSync(p);
    } catch {
      continue;
    }
    if (s.isDirectory()) out = out.concat(walk(p));
    else out.push(p);
  }
  return out;
}

const files = walk(root);
const cobertura = files.filter((f) => f.endsWith('coverage.cobertura.xml'));
const opencover = files.filter((f) => f.endsWith('coverage.opencover.xml'));

if (cobertura.length === 0) {
  fail(
    `no coverage.cobertura.xml under ${JSON.stringify(root)}. The collector ` +
      'produced nothing, yet dotnet test exited 0 - which is exactly the silent ' +
      'failure this gate exists to catch.'
  );
}

let totalValid = 0;
let totalCovered = 0;
const packages = new Set();

for (const path of cobertura) {
  const xml = readFileSync(path, 'utf8');

  // Only two things are read out of this file - the root element's line totals
  // and the package names - so a narrow regex is honest here and avoids a
  // dependency. It is deliberately strict: a file that does not present these
  // attributes is treated as not being a coverage report at all, rather than
  // being silently scored as zero.
  const rootTag = xml.match(/<coverage\b[^>]*>/i);
  if (!rootTag) fail(`${path} has no <coverage> root element; it is not a Cobertura report.`);

  const num = (name) => {
    const m = rootTag[0].match(new RegExp(`${name}="([0-9]+)"`, 'i'));
    if (!m) fail(`${path} is missing the ${name} attribute. A report that does not say how much it measured cannot be used to prove anything was measured.`);
    return Number(m[1]);
  };

  totalValid += num('lines-valid');
  totalCovered += num('lines-covered');

  for (const [, name] of xml.matchAll(/<package\b[^>]*\bname="([^"]*)"/gi)) {
    packages.add(name);
  }
}

console.log(`reports      : ${cobertura.length}`);
console.log(`packages     : ${[...packages].sort().join(', ') || '(none)'}`);
console.log(`lines valid  : ${totalValid}`);
console.log(`lines covered: ${totalCovered}`);
if (totalValid) console.log(`line rate    : ${((100 * totalCovered) / totalValid).toFixed(1)}%`);

if (totalValid < MIN_LINES_VALID) {
  fail(
    `the report accounts for only ${totalValid} measurable lines (floor is ` +
      `${MIN_LINES_VALID}). An almost-empty report is indistinguishable from an ` +
      'honest 0% on a dashboard, which is why this is an error and not a warning.'
  );
}

for (const want of EXPECT_PACKAGES) {
  if (![...packages].some((p) => p.includes(want))) {
    fail(
      `no package matching ${JSON.stringify(want)} was measured. The collector ` +
        'attached to something, but not to the product.'
    );
  }
}

for (const forbid of FORBID_PACKAGES) {
  if (packages.has(forbid)) {
    fail(
      `the test assembly ${JSON.stringify(forbid)} appears in the coverage ` +
        'report. The Exclude in .runsettings was not applied, so every figure ' +
        'here counts the tests testing themselves.'
    );
  }
}

if (opencover.length === 0) {
  fail(
    'a Cobertura report exists but no coverage.opencover.xml does. OpenCover is ' +
      "the only format SonarQube imports for C#, so losing it would disconnect " +
      'coverage from analysis without any other symptom.'
  );
}

console.log('PASS: coverage was collected, and it measured the product rather than the tests.');

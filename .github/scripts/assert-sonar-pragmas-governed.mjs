/**
 * Govern in-source suppressions of SonarQube rules.
 *
 * WHY THIS EXISTS
 *
 * `#pragma warning disable S<digits>` is honoured by the Sonar C# analyser, and
 * it is the right mechanism for a per-site false positive: the reason sits where
 * the next reader will meet it, and it MOVES WITH THE CODE. A path-scoped entry
 * in a config file silently stops matching the day someone renames the file, and
 * a suppression that silently stops matching is worse than none - the finding
 * comes back, the disposition is gone, and nothing says so.
 *
 * But that convenience is exactly the hazard. A pragma is one line and needs no
 * review; a config entry costs a pull request. That asymmetry is fine for
 * CS1591 and not at all fine for a rule a quality gate counts, because it lets
 * a real finding be disposed of with no argument and no record. A peer effort on
 * the engine repository (STP-743) found 102 existing pragmas there, most
 * carrying no reason whatsoever.
 *
 * This repository currently has exactly ONE Sonar-rule pragma. Installing the
 * rule now, while the count is one, is the whole point: it is cheap to comply
 * with from the start and expensive to retrofit onto a hundred.
 *
 * WHAT IS REQUIRED, AND WHY EACH PART
 *
 *   1. A SONAR-DISPOSITION marker naming the rule.
 *      Makes every suppression greppable in one command, so the set can be
 *      reviewed as a set rather than discovered file by file.
 *
 *   2. A REVIEW date, in the future.
 *      This closes a gap the peer effort named explicitly and did not close:
 *      neither pragmas nor config suppressions expire. hardened-ci.yml claims
 *      no finding can be suppressed indefinitely without someone re-arguing it.
 *      That is true of the dependency gates and was false of this mechanism.
 *      A dated review makes the claim true: when the date passes, the build
 *      fails and a human has to either renew the argument or delete it. An
 *      undated suppression is a decision that outlives everyone who understood
 *      it.
 *
 *   3. A matching `restore`.
 *      Without it the disable runs to end of file, silently covering code
 *      nobody intended to exempt. The suppression must be scoped to the thing
 *      that was argued about.
 *
 * WHAT THIS DOES NOT DO
 *
 * It does not judge whether a disposition is CORRECT - no checker can. It
 * enforces that one was made, is attributable, is bounded in time, and is
 * scoped. Whether the reasoning holds is what the review date forces someone
 * to revisit.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const SKIP_DIRS = new Set(['bin', 'obj', '.git', 'node_modules', '.sonarqube', 'artifacts']);

// A Sonar rule id: S followed by digits. Deliberately NOT matching CS####,
// CA####, IDE#### - those are compiler and Roslyn analyser warnings, they are
// not counted by the quality gate, and governing them here would be noise that
// devalues the rule.
const DISABLE = /#pragma\s+warning\s+disable\s+(S\d+)\b/;
const RESTORE = /#pragma\s+warning\s+restore\s+(S\d+)\b/;
const MARKER = /SONAR-DISPOSITION:\s*(S\d+)\b/;
const REVIEW = /REVIEW:\s*(\d{4})-(\d{2})-(\d{2})\b/;

// How far above the pragma to look for its marker and date. Generous, because a
// good justification is several lines of prose, and stingy enough that the
// annotation cannot drift onto an unrelated member.
const LOOKBACK = 40;

const root = process.argv[2] ?? '.';
// Injectable so the expiry control is deterministic rather than a bet on the
// clock. A test that only fails after a real date passes is a test nobody runs.
const today = process.argv[3] ?? new Date().toISOString().slice(0, 10);

const problems = [];
let examined = 0;

function walk(dir, out = []) {
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
    if (s.isDirectory()) {
      if (!SKIP_DIRS.has(e)) walk(p, out);
    } else if (e.endsWith('.cs')) {
      out.push(p);
    }
  }
  return out;
}

for (const file of walk(root)) {
  const text = readFileSync(file, 'utf8');
  if (!DISABLE.test(text)) continue;
  const lines = text.split(/\r?\n/);
  const where = relative(root, file).split(sep).join('/');

  const restored = new Set();
  for (const l of lines) {
    const m = l.match(RESTORE);
    if (m) restored.add(m[1]);
  }

  lines.forEach((line, i) => {
    const m = line.match(DISABLE);
    if (!m) return;
    const rule = m[1];
    examined++;
    const at = `${where}:${i + 1} (${rule})`;
    const above = lines.slice(Math.max(0, i - LOOKBACK), i).join('\n');

    const marker = above.match(MARKER);
    if (!marker) {
      problems.push(
        `${at}: no "SONAR-DISPOSITION: ${rule}" marker in the ${LOOKBACK} lines above. ` +
          'A suppression without a stated reason is a finding deleted rather than answered.'
      );
    } else if (marker[1] !== rule) {
      problems.push(
        `${at}: the nearest SONAR-DISPOSITION names ${marker[1]}, not ${rule}. ` +
          'The justification belongs to a different rule - one of the two is misplaced.'
      );
    }

    const rev = above.match(REVIEW);
    if (!rev) {
      problems.push(
        `${at}: no "REVIEW: YYYY-MM-DD" date. Suppressions must expire, or they ` +
          'outlive everyone who understood why they were added.'
      );
    } else {
      const date = `${rev[1]}-${rev[2]}-${rev[3]}`;
      if (Number.isNaN(Date.parse(date))) {
        problems.push(`${at}: REVIEW date ${date} is not a real date.`);
      } else if (date < today) {
        problems.push(
          `${at}: REVIEW date ${date} has passed (today is ${today}). Re-argue this ` +
            'suppression and move the date, or remove it and fix the finding.'
        );
      }
    }

    if (!restored.has(rule)) {
      problems.push(
        `${at}: no matching "#pragma warning restore ${rule}" in this file, so the ` +
          'suppression runs to end of file and covers code nobody argued about.'
      );
    }
  });
}

console.log(`Examined ${examined} Sonar-rule pragma(s) under ${root}`);

if (problems.length) {
  console.error('');
  for (const p of problems) console.error(`  ${p}`);
  console.error('');
  console.error(`FAIL: ${problems.length} governance problem(s).`);
  console.error('');
  console.error('A Sonar-rule suppression must carry, within 40 lines above it:');
  console.error('    // SONAR-DISPOSITION: S#### why this finding does not apply here');
  console.error('    // REVIEW: YYYY-MM-DD');
  console.error('and a matching "#pragma warning restore S####" to bound its scope.');
  process.exit(1);
}

console.log('PASS: every Sonar-rule suppression is justified, dated, and scoped.');

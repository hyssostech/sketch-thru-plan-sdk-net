#!/usr/bin/env node
/*
 * STP-1064. Re-pins the shared call-validation rules that StpSDK.Validation evaluates.
 *
 * The rule file and its schema live next to the public contract in
 * hyssostech/sketch-thru-plan-sdk-js (json-api/). This SDK embeds a VERBATIM copy of the rule
 * file, and records where it came from in sketch-thru-plan-validations.pin.json: repo, path,
 * commit, version, contractVersion and the SHA-256 of each file (LF line endings). The tests
 * check the embedded copy against that record, so a hand edit, or a copy without a pin, fails.
 *
 * Usage: node scripts/update-call-validations.mjs <commit-sha> [repoRoot]
 *   Pin a full 40-character commit of sketch-thru-plan-sdk-js that is on its main branch.
 *   Then run the tests, and read the diff of the rule file: a new major version needs
 *   evaluator changes first (StpCallValidations.SupportedMajorVersion).
 */
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const REPO = 'hyssostech/sketch-thru-plan-sdk-js';
const RULES = 'json-api/sketch-thru-plan-validations.json';
const SCHEMA = 'json-api/sketch-thru-plan-validations.schema.json';

const [ref, root = '.'] = process.argv.slice(2);
if (!/^[0-9a-f]{40}$/.test(ref ?? '')) {
  console.error('usage: node scripts/update-call-validations.mjs <40-character commit sha> [repoRoot]');
  process.exit(2);
}
const dir = join(root, 'src', 'StpSDK.JsonRpc', 'Validation');

async function fetchText(path) {
  const url = `https://raw.githubusercontent.com/${REPO}/${ref}/${path}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error(`${url}: HTTP ${res.status}`);
  return (await res.text()).replace(/\r\n/g, '\n');
}

const sha256 = (text) => createHash('sha256').update(text, 'utf8').digest('hex');

const rulesText = await fetchText(RULES);
const schemaText = await fetchText(SCHEMA);
const rules = JSON.parse(rulesText);
JSON.parse(schemaText);

writeFileSync(join(dir, 'sketch-thru-plan-validations.json'), rulesText);
writeFileSync(join(dir, 'sketch-thru-plan-validations.schema.json'), schemaText);
const pinPath = join(dir, 'sketch-thru-plan-validations.pin.json');
const before = JSON.parse(readFileSync(pinPath, 'utf8'));
const pin = {
  repo: REPO,
  path: RULES,
  schemaPath: SCHEMA,
  ref,
  version: rules.version,
  contractVersion: rules.contractVersion,
  sha256: sha256(rulesText),
  schemaSha256: sha256(schemaText),
};
writeFileSync(pinPath, JSON.stringify(pin, null, 2) + '\n');
console.log(`pinned ${REPO}@${ref}: rules ${before.version} -> ${pin.version} (contract ${pin.contractVersion})`);

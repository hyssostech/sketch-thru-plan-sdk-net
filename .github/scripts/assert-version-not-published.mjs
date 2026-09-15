#!/usr/bin/env node
/*
 * STP-699 Phase 3. A release that cannot silently do nothing.
 *
 * `dotnet nuget push --skip-duplicate` exits 0 when the version is already on
 * nuget.org. That is correct as a RETRY safety net - a re-run of a half-finished
 * publish should not fail - and wrong as the only outcome, because a tag cut
 * against a csproj whose <Version> was never bumped produces a completely green
 * workflow that published nothing at all. That failure mode is not theoretical
 * in this repository; it is why five merged fixes sat unpublished behind
 * 0.4.2-preview.
 *
 * publish.yml already asserts the tag agrees with the packed version. This
 * asserts the other half: that the version is not already out there.
 *
 * FAILS CLOSED. If nuget.org cannot be reached, or answers something that is not
 * the expected document, this exits non-zero. An unverifiable publish is not a
 * safe publish, and a registry that is down is one you cannot push to anyway.
 *
 * Usage:
 *   node assert-version-not-published.mjs <packageId> <version> [--index <url|path>]
 *
 * --index takes a file path so the controls can exercise every branch without a
 * network, and defaults to the nuget.org flat-container index for the package.
 */
const [, , packageId, version, ...rest] = process.argv;

if (!packageId || !version) {
  console.error('usage: assert-version-not-published.mjs <packageId> <version> [--index <url|path>]');
  process.exit(2);
}

let index = null;
for (let i = 0; i < rest.length; i += 1) {
  if (rest[i] === '--index') index = rest[i + 1];
}
if (!index) {
  // Flat container ids are lower-cased; the service 404s on the cased form.
  index = `https://api.nuget.org/v3-flatcontainer/${packageId.toLowerCase()}/index.json`;
}

const die = (msg) => {
  console.error(`::error::${msg}`);
  process.exit(1);
};

// node:https rather than the global fetch. undici holds a keep-alive socket
// open, and process.exit() while it is open aborts the process on Windows
// ("Assertion failed: !(handle->flags & UV_HANDLE_CLOSING)"), turning a
// PASSING check into exit 127 - a release blocked for no reason. Measured, not
// guessed: the failure branch survived it only because 127 is also non-zero.
function get(url, redirectsLeft = 3) {
  return new Promise((resolve, reject) => {
    import('node:https').then(({ get: httpsGet }) => {
      const req = httpsGet(url, { headers: { accept: 'application/json' } }, (res) => {
        const { statusCode, headers } = res;
        if ([301, 302, 307, 308].includes(statusCode) && headers.location) {
          res.resume();
          if (redirectsLeft === 0) return reject(new Error(`too many redirects from ${url}`));
          return resolve(get(new URL(headers.location, url).toString(), redirectsLeft - 1));
        }
        let body = '';
        res.setEncoding('utf8');
        res.on('data', (chunk) => {
          body += chunk;
        });
        res.on('end', () => resolve({ statusCode, body }));
      });
      req.on('error', reject);
      req.setTimeout(30000, () => req.destroy(new Error('timed out after 30s')));
    }, reject);
  });
}

async function load(src) {
  if (/^https?:\/\//.test(src)) {
    const { statusCode, body } = await get(src);
    // 404 is the documented answer for a package id that has never been
    // published. It is a legitimate "not there", not a failure.
    if (statusCode === 404) return { versions: [], firstEver: true };
    if (statusCode !== 200) {
      die(`${src} answered HTTP ${statusCode}. Cannot verify, so not publishing.`);
    }
    return JSON.parse(body);
  }
  const { readFileSync } = await import('node:fs');
  return JSON.parse(readFileSync(src, 'utf8'));
}

let doc;
try {
  doc = await load(index);
} catch (e) {
  die(`could not read ${index}: ${e.message}. Cannot verify, so not publishing.`);
}

if (!doc || !Array.isArray(doc.versions)) {
  die(
    `${index} did not answer with a {"versions":[...]} document. ` +
      'Cannot verify, so not publishing.'
  );
}

const published = doc.versions;

if (doc.firstEver) {
  console.log(`${packageId} has never been published. ${version} is clear.`);
  process.exit(0);
}

// A registry that answered with an EMPTY list for a package that exists is not
// a clean bill of health, it is a broken answer - and it would wave through any
// version at all.
if (published.length === 0) {
  die(
    `${index} returned an empty version list for ${packageId}, which already has ` +
      'releases. That is not an answer this check can act on.'
  );
}

// NuGet version comparison is case-insensitive on the pre-release label.
const hit = published.find((v) => v.toLowerCase() === version.toLowerCase());

console.log(`${packageId}: ${published.length} version(s) already on the registry.`);

if (hit) {
  die(
    `${packageId} ${hit} is ALREADY published. This run would push nothing and ` +
      'still go green (--skip-duplicate). Bump <Version> in the csproj, or drop ' +
      'the tag if it was cut by mistake. NuGet has no delete, only unlist, so a ' +
      'version that is out stays out.'
  );
}

console.log(`${version} is not published yet. Clear to publish.`);

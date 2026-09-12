# Verifying a HyssosTech.Sdk.STP release

Every check below is paired with what it does **not** prove. That pairing is the
point of this document. Each command answers one narrow question, and the usual
way supply-chain verification fails is not that a check returns the wrong answer
- it is that someone reads a passing check as an answer to a broader question
than the one it asked.

Jira STP-711.

---

## 1. The package signature, straight from the registry

Every package on nuget.org carries a **repository signature** applied by
nuget.org itself. You do not need a release to check this - it works against
anything you have already restored.

```sh
# Download without installing, then verify
nuget install HyssosTech.Sdk.STP -Version <version> -OutputDirectory ./pkg
dotnet nuget verify ./pkg/HyssosTech.Sdk.STP.<version>/HyssosTech.Sdk.STP.<version>.nupkg
```

On Windows with the classic client:

```sh
nuget verify -Signatures HyssosTech.Sdk.STP.<version>.nupkg
```

**What this proves.** The bytes you hold are the bytes nuget.org recorded, and
they have not been altered since nuget.org signed them.

**What it does NOT prove.** It says nothing about where the package came from
before nuget.org received it, nor that the source it was built from is the
source in this repository. A repository signature is a statement by the
registry about custody, not about provenance.

---

## 2. Build provenance for a GitHub Release

Releases cut by `.github/workflows/release.yml` attach a `SHA256SUMS` manifest
and a **SLSA build provenance attestation over that manifest**, minted by GitHub
from the workflow's OIDC identity.

```sh
gh release download <tag> --repo hyssostech/sketch-thru-plan-sdk-net
gh attestation verify SHA256SUMS --repo hyssostech/sketch-thru-plan-sdk-net
sha256sum -c SHA256SUMS
```

The two steps are both required and neither substitutes for the other:

- `gh attestation verify` establishes that **this manifest** was produced by
  that workflow, in that repository, at a specific commit.
- `sha256sum -c` establishes that **the files you downloaded** are the files
  that manifest describes.

**Why the attestation names SHA256SUMS and not the package.** The attestation
subject is the manifest, so `gh attestation verify HyssosTech.Sdk.STP.<version>.nupkg`
will report "no attestations found". That is expected, not a failure. The
manifest is the binding: verify it, then verify the files against it.

**What this proves.** A specific workflow run, on a specific commit of this
repository, produced exactly these bytes.

**What it does NOT prove.** That the commit was reviewed, that the source is
free of defects, or that the dependencies it pulled in are safe. Provenance
answers "who built it and from what", never "is it good".

---

## 3. Source, from the symbols

The package is built with SourceLink and ships a symbol package:

- `Deterministic` - the same source and inputs produce the same output
- `PublishRepositoryUrl` and `EmbedUntrackedSources` - the symbols carry the
  repository and commit they came from
- `IncludeSymbols` with `SymbolPackageFormat=snupkg` - a `.snupkg` accompanies
  every published version

So a debugger can step from a compiled method into the exact source revision,
and you can read the commit out of the symbols rather than taking our word for
it.

**What this proves.** The binary you are running corresponds to a named commit
in a named repository.

**What it does NOT prove.** That the commit is on `main`, that it was reviewed,
or that the published binary was built from it by anyone in particular -
SourceLink records a claim made at build time, and only the attestation in
section 2 independently binds a build to a workflow.

---

## 4. The SBOM and its scan

Each release attaches a CycloneDX SBOM and the Trivy report taken over it.

```sh
gh release download <tag> --repo hyssostech/sketch-thru-plan-sdk-net
# inspect the component list
python -c "import json;d=json.load(open('sbom.cdx.json'));print(len(d.get('components',[])),'components')"
```

The SBOM is generated from the **shipped project** with `--exclude-test-projects`
and `--exclude-dev`, so it describes what you install rather than what we build
with. The assertion in `.github/scripts/assert-sbom.py` fails the release if the
spec version, the package name or the component count is not what was asked for
- an SBOM that silently describes nothing is worse than none.

The scan runs **without** `--ignore-unfixed`. That flag answers "is there an
upstream fix available", not "are we exposed", and dropping unfixed advisories
is precisely how a release can claim zero findings while shipping a known one.

**What this proves.** The dependency set at build time, and which known
advisories applied to it **on the day the scan ran**.

**What it does NOT prove.** That the set is safe today. A vulnerability
published after the release exists in the artifact and not in the report. An
advisory verdict has a timestamp; treat an old scan as history, not as status.

---

## 5. What is NOT covered

Stated plainly so it is not mistaken for an omission:

- **No cosign signature over the package.** The integrity chain here is the
  nuget.org repository signature plus the GitHub attestation over `SHA256SUMS`.
- **No air-gapped verification path.** Every command above reaches the network.
- **Release assets only.** Sections 2 and 4 apply to releases cut by
  `release.yml`. Versions published before it landed have the registry
  signature and SourceLink, but no attestation and no attached SBOM.

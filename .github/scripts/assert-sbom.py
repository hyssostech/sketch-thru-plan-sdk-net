#!/usr/bin/env python3
"""Assert the CycloneDX SBOM is scannable, scoped and honest about itself.

STP-730 / sdk-net parity. Ported from the STP engine's PowerShell assertion
(hardened-ci.yml "Verify SBOM is scannable and scoped"), which exists because
all three of these failed SILENTLY in that repo's rc5:

  - specVersion drifted to 1.7, which Trivy 0.70 cannot decode, so the SBOM
    was unscannable while every check stayed green;
  - the component version stamped 0.0.0, so the document did not say which
    build it described;
  - `--exclude-test-projects` missed projects (cyclonedx-dotnet#669), so
    test-only packages shipped inside a customer-facing SBOM.

A generator reporting success is not evidence that it produced a usable
document. This is that evidence.

Usage: assert-sbom.py <sbom.json> <expected-spec> <expected-version>
"""
import io
import json
import re
import sys

TEST_ONLY = re.compile(
    r"^(xunit|nunit|moq|coverlet|Microsoft\.NET\.Test\.Sdk|FluentAssertions"
    r"|NSubstitute|Shouldly|AutoFixture|Verify)",
    re.IGNORECASE,
)


def main(argv):
    if len(argv) != 4:
        print("usage: assert-sbom.py <sbom.json> <expected-spec> <expected-version>")
        return 2
    path, want_spec, want_version = argv[1], argv[2], argv[3]

    try:
        bom = json.load(io.open(path, encoding="utf-8"))
    except Exception as exc:
        print("::error::SBOM: cannot parse %s: %s" % (path, exc))
        return 1

    problems = []

    spec = bom.get("specVersion")
    if spec != want_spec:
        problems.append(
            "specVersion is %r, expected %r - a scanner that cannot decode the "
            "document reports no findings, which reads exactly like clean"
            % (spec, want_spec)
        )

    meta = (bom.get("metadata") or {}).get("component") or {}
    version = (meta.get("version") or "").strip()
    if not version or version == "0.0.0":
        problems.append(
            "metadata.component.version is %r - the SBOM does not say which "
            "build it describes" % version
        )
    elif version != want_version:
        problems.append(
            "metadata.component.version is %r but the packed version is %r - "
            "the SBOM describes a different build than the one shipping"
            % (version, want_version)
        )

    components = bom.get("components") or []
    if not components:
        problems.append(
            "zero components - an empty SBOM satisfies every 'no vulnerabilities' "
            "check while describing nothing"
        )

    leaked = sorted({c.get("name", "") for c in components
                     if TEST_ONLY.match(c.get("name") or "")})
    if leaked:
        problems.append(
            "test-only packages present in a shipped SBOM: %s "
            "(--exclude-test-projects is known unreliable)" % ", ".join(leaked)
        )

    if problems:
        for p in problems:
            print("::error::SBOM: %s" % p)
        return 1

    print("SBOM OK: spec %s, version %s, %d components, no test packages"
          % (spec, version, len(components)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

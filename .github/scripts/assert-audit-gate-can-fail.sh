#!/usr/bin/env bash
#
# Re-prove that the NuGet audit gate CAN FAIL.
#
# WHY THIS EXISTS
#
# Directory.Build.props and Directory.Solution.targets were each proven to fail
# before being trusted green. That proof lived in a commit message and a Jira
# comment - which is to say it was not recorded at all. Nothing in the
# repository re-ran it, so the gate could narrow or stop running and every
# check would stay green:
#
#   - flip NuGetAuditMode to 'direct' and transitive advisories go unseen
#     (the default below net10.0, and the exact hole STP-703 was filed for)
#   - set NuGetAudit=false and nothing is audited at all
#   - cache obj/ and restore skips every project, auditing none of them
#
# An independent review found the third of those in the gate itself. Prose is
# not a mechanism. This script is.
#
# It asserts FAILURE, not success. Each case below must exit non-zero; if any
# of them starts passing, the gate has stopped gating and this script says so.
#
# Deliberately NOT set -e: several commands are expected to fail.
set -uo pipefail

SLN="${1:-StpSDK.sln}"
PROBE_DIR=".audit-gate-probe"
rc=0

pass() { printf '  PASS  %s\n' "$1"; }
fail() { printf '  FAIL  %s\n' "$1" >&2; rc=1; }

cleanup() { rm -rf "$PROBE_DIR"; }
trap cleanup EXIT

printf 'Audit-gate self-test against %s\n\n' "$SLN"

# ---------------------------------------------------------------------------
# 1. A REAL advisory must fail the build.
#
# The strongest proof, because it uses a genuine known-bad input rather than a
# switch. System.Text.Json 8.0.4 is inside GHSA-8g4q-xg66-9fp4 (High, affects
# >= 8.0.0 <= 8.0.4). The probe sits inside the repository so it inherits
# Directory.Build.props by directory walk - which is also what makes this a
# test OF the props file rather than of a copy of its settings.
# ---------------------------------------------------------------------------
mkdir -p "$PROBE_DIR"
cat >"$PROBE_DIR/probe.csproj" <<'PROBE'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <!-- Deliberately vulnerable: GHSA-8g4q-xg66-9fp4, High. -->
    <PackageReference Include="System.Text.Json" Version="8.0.4" />
  </ItemGroup>
</Project>
PROBE

out="$(dotnet restore "$PROBE_DIR/probe.csproj" --force 2>&1)"
if [ $? -eq 0 ]; then
  fail "a High advisory (System.Text.Json 8.0.4) did NOT fail the restore - the gate is not gating"
elif ! printf '%s' "$out" | grep -q 'NU1903'; then
  fail "restore failed but not with NU1903 - check the failure is the advisory and not something else"
else
  pass "a real High advisory fails the build (NU1903 as error)"
fi
cleanup

# ---------------------------------------------------------------------------
# 2. Auditing nothing must fail, not pass quietly.
# ---------------------------------------------------------------------------
out="$(CI=true dotnet restore "$SLN" --force -p:NuGetAudit=false 2>&1)"
if [ $? -eq 0 ]; then
  fail "NuGetAudit=false still exited 0 - the coverage assertion is not running"
else
  pass "NuGetAudit=false fails the coverage assertion"
fi

# ---------------------------------------------------------------------------
# 3. Unpopulated counts must fail rather than pass vacuously.
#
# Static graph restore does not emit the audit counts. The naive form of the
# assertion compares '' against '' and passes - verifying nothing.
# ---------------------------------------------------------------------------
out="$(CI=true dotnet restore "$SLN" --force -p:RestoreUseStaticGraphEvaluation=true 2>&1)"
if [ $? -eq 0 ]; then
  fail "static graph restore exited 0 - empty audit counts are passing vacuously"
else
  pass "unpopulated audit counts fail explicitly"
fi

# ---------------------------------------------------------------------------
# 4. A no-op restore audits nothing and must fail under CI.
#
# This is the one an independent review found. NuGet does not audit projects it
# skips, so 'audited + skipped == total' passed at ZERO coverage. Add
# actions/cache over obj/ and the gate silently stops auditing.
# ---------------------------------------------------------------------------
CI=true dotnet restore "$SLN" --force >/dev/null 2>&1
out="$(CI=true dotnet restore "$SLN" 2>&1)"
if [ $? -eq 0 ]; then
  fail "a no-op restore exited 0 - the gate passes at zero coverage when restore is cached"
else
  pass "a no-op restore fails (skipped projects are not audited)"
fi

# ---------------------------------------------------------------------------
# 5. The healthy case must pass AND say what it verified.
#
# A gate whose success is silent cannot be distinguished from one that never
# ran, so the coverage line is part of the contract, not decoration.
# ---------------------------------------------------------------------------
out="$(CI=true dotnet restore "$SLN" --force 2>&1)"
if [ $? -ne 0 ]; then
  fail "a clean restore did NOT pass - the gate is failing something it should not"
elif ! printf '%s' "$out" | grep -q 'NuGet audit verified:'; then
  fail "the clean restore passed SILENTLY - success is indistinguishable from the check not running"
elif printf '%s' "$out" | grep -qE 'NuGet audit verified: 0 audited'; then
  fail "the clean restore reported 0 audited while passing"
else
  pass "$(printf '%s' "$out" | grep -o 'NuGet audit verified:.*')"
fi

printf '\n'
if [ "$rc" -ne 0 ]; then
  printf 'AUDIT GATE SELF-TEST FAILED. The gate is not proven to fail, so a green\n'
  printf 'restore elsewhere in CI is not evidence that anything was audited.\n' >&2
else
  printf 'Audit gate self-test passed: the gate was shown to fail on 4 distinct\n'
  printf 'inputs and to pass, loudly, on a clean one.\n'
fi
exit "$rc"

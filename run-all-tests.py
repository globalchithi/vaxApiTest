#!/usr/bin/env python3
"""
SpecFlow Test Runner

Runs the SpecFlow test project, generates a LivingDoc report, and opens it (optional).
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SPECFLOW_PROJECT = ROOT / "SpecFlowTests" / "SpecFlowTests.csproj"


def run_command(cmd: list[str], *, env: dict[str, str] | None = None, cwd: Path | None = None) -> None:
    """Execute a command and stop on failure."""
    print(f"\n> {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=cwd or ROOT, env=env)
    if result.returncode != 0:
        raise RuntimeError(f"Command failed with exit code {result.returncode}: {' '.join(cmd)}")


def ensure_livingdoc(env: dict[str, str]) -> str | None:
    """Return an executable command for LivingDoc, or None if unavailable."""
    # 1. PATH lookup
    livingdoc_cmd = shutil.which("livingdoc", path=env.get("PATH"))
    if livingdoc_cmd:
        return livingdoc_cmd

    # 2. Direct path to dotnet tools directory
    tools_exe = Path(env["USERPROFILE"] if os.name == "nt" else Path.home()) / ".dotnet" / "tools" / "livingdoc.exe"
    if tools_exe.exists():
        return str(tools_exe)

    tools_cmd = Path(env["USERPROFILE"] if os.name == "nt" else Path.home()) / ".dotnet" / "tools" / "livingdoc"
    if tools_cmd.exists():
        return str(tools_cmd)

    # 3. `dotnet tool run livingdoc`
    return None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Run SpecFlow suite and generate LivingDoc report.")
    parser.add_argument("--base-url", default="https://vhapistg.vaxcare.com", help="API base URL for the tests.")
    parser.add_argument("--environment", default=None, help="TEST_ENVIRONMENT value (optional).")
    parser.add_argument("--configuration", default="Debug", help="Build configuration (Debug/Release).")
    parser.add_argument("--framework", default="net8.0", help="Target framework moniker.")
    parser.add_argument("--no-report", action="store_true", help="Skip LivingDoc report generation.")
    parser.add_argument("--open-report", action="store_true", help="Open LivingDoc HTML after generation.")
    parser.add_argument("--logger", default="trx", help="Additional logger passed to dotnet test.")
    args = parser.parse_args(argv)

    if not SPECFLOW_PROJECT.exists():
        print(f"❌ SpecFlow project not found: {SPECFLOW_PROJECT}")
        return 1

    env = os.environ.copy()
    env["API_BASE_URL"] = args.base_url
    if args.environment:
        env["TEST_ENVIRONMENT"] = args.environment

    dotnet_test_cmd = ["dotnet", "test", str(SPECFLOW_PROJECT)]
    if args.logger:
        dotnet_test_cmd.extend(["--logger", args.logger])

    try:
        run_command(dotnet_test_cmd, env=env)
    except RuntimeError as exc:
        print(f"\n❌ dotnet test failed: {exc}")
        return 1

    custom_report = ROOT / "SpecFlowTests" / "TestResults" / "CustomReport.html"

    if args.no_report:
        print("\nℹ️  Report generation skipped (--no-report).")
        return 0

    dll_path = ROOT / "SpecFlowTests" / "bin" / args.configuration / args.framework / "SpecFlowTests.dll"
    test_execution_json = dll_path.parent / "TestExecution.json"
    output_html = ROOT / "SpecFlowTests" / "TestResults" / "LivingDoc.html"
    output_html.parent.mkdir(parents=True, exist_ok=True)

    if not dll_path.exists():
        print(f"\n⚠️  SpecFlow assembly not found at {dll_path}. Cannot run LivingDoc.")
        return 0

    livingdoc_exec = ensure_livingdoc(env)

    if livingdoc_exec:
        livingdoc_cmd = [
            livingdoc_exec,
            "test-assembly",
            str(dll_path),
            "-t",
            str(test_execution_json),
            "--output",
            str(output_html),
        ]
        try:
            run_command(livingdoc_cmd, env=env)
        except RuntimeError as exc:
            print(f"\n⚠️  LivingDoc command failed: {exc}")
            print("    Tip: try running with elevated permissions or using 'dotnet tool run livingdoc -- ...'")
            return 1
    else:
        print("\n⚠️  LivingDoc CLI not found on PATH.")
        print("    - Install: dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI")
        print("    - Ensure %USERPROFILE%\\.dotnet\\tools is on PATH (PowerShell: $env:PATH = ...)")
        print("    - Alternative: dotnet tool run livingdoc -- test-assembly ...")
        return 1

    print(f"\n✅ LivingDoc report generated at {output_html}")

    # Generate custom HTML report via C#
    try:
        run_command(
            [
                "dotnet",
                "run",
                "--project",
                str(ROOT / "tools" / "SpecFlowReportGenerator" / "SpecFlowReportGenerator.csproj"),
                "--",
                str(test_execution_json),
                str(custom_report),
            ],
            env=env,
        )
        print(f"✅ Custom report generated at {custom_report}")
    except RuntimeError as exc:
        print(f"\n⚠️  Custom report generation failed: {exc}")

    # Generate TRX summary (works even if TestExecution.json missing)
    trx_file = ROOT / "SpecFlowTests" / "TestResults" / "SpecFlow.trx"
    if trx_file.exists():
        try:
            run_command(
                [
                    "dotnet",
                    "run",
                    "--project",
                    str(ROOT / "tools" / "TrxReportGenerator" / "TrxReportGenerator.csproj"),
                    "--",
                    str(trx_file),
                    str(ROOT / "SpecFlowTests" / "TestResults" / "TrxSummary.html"),
                ],
                env=env,
            )
            print(f"✅ TRX summary report generated at {ROOT / 'SpecFlowTests' / 'TestResults' / 'TrxSummary.html'}")
        except RuntimeError as exc:
            print(f"\n⚠️  TRX summary generation failed: {exc}")
    else:
        print("⚠️  No SpecFlow.trx found; skipping TRX summary.")

    if args.open_report:
        try:
            if sys.platform.startswith("darwin"):
                run_command(["open", str(output_html)])
                if custom_report.exists():
                    run_command(["open", str(custom_report)])
                trx_summary = ROOT / "SpecFlowTests" / "TestResults" / "TrxSummary.html"
                if trx_summary.exists():
                    run_command(["open", str(trx_summary)])
            elif os.name == "nt":
                os.startfile(str(output_html))  # type: ignore[arg-type]
                if custom_report.exists():
                    os.startfile(str(custom_report))  # type: ignore[arg-type]
                trx_summary = ROOT / "SpecFlowTests" / "TestResults" / "TrxSummary.html"
                if trx_summary.exists():
                    os.startfile(str(trx_summary))  # type: ignore[arg-type]
            else:
                run_command(["xdg-open", str(output_html)])
                if custom_report.exists():
                    run_command(["xdg-open", str(custom_report)])
        except Exception as exc:  # pragma: no cover - best-effort
            print(f"⚠️  Failed to open report automatically: {exc}")

    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("\n🛑 Test execution interrupted by user")
        sys.exit(1)
#!/usr/bin/env python3
"""
Build WorkflowForge documentation the same way GitHub Pages CI does:

  1. dotnet build (Release) — XML documentation for DocFX
  2. sync chrome + dotnet docfx metadata + build — writes docs/api/
  3. flatten API URLs (api/api/ → api/)
  4. bundle exec jekyll build — writes docs/_site/
  5. append API pages to sitemap.xml

Generated folders docfx/api/ and docs/api/ are gitignored; run this script locally
or via .github/workflows/pages.yml before preview or deploy.

Examples:
  python scripts/build_docs.py
  python scripts/build_docs.py --serve
  python scripts/build_docs.py --baseurl /workflow-forge
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _run(cmd: list[str], cwd: Path | None = None, env: dict[str, str] | None = None) -> None:
    merged = os.environ.copy()
    if env:
        merged.update(env)
    print(f"+ {' '.join(cmd)}", flush=True)
    subprocess.run(cmd, cwd=cwd, env=merged, check=True)


def _require_tool(name: str) -> str:
    path = shutil.which(name)
    if path is None:
        print(f"error: '{name}' not found on PATH", file=sys.stderr)
        sys.exit(1)
    return path


def _run_py(root: Path, script: str, *args: str) -> None:
    _run([sys.executable, str(root / "scripts" / script), *args], cwd=root)


def build_api_docs(root: Path, configuration: str, skip_dotnet_build: bool) -> None:
    _run_py(root, "sync_docfx_chrome.py")
    if not skip_dotnet_build:
        _run(
            ["dotnet", "build", "WorkflowForge.sln", "-c", configuration],
            cwd=root,
        )
    _run(["dotnet", "tool", "restore"], cwd=root)
    docfx_dir = root / "docfx"
    _run(["dotnet", "docfx", "metadata", "docfx.json"], cwd=docfx_dir)
    _run(["dotnet", "docfx", "build", "docfx.json"], cwd=docfx_dir)
    _run_py(root, "flatten_docfx_api_urls.py")


def _docker_workspace_path(path: Path) -> str:
    """Convert path for Docker volume mounts (Windows-friendly)."""
    resolved = path.resolve()
    posix = resolved.as_posix()
    if len(posix) >= 2 and posix[1] == ":":
        return "/" + posix[0].lower() + posix[2:]
    return posix


def build_jekyll_site(root: Path, baseurl: str, serve: bool, use_docker: bool) -> None:
    docs_dir = root / "docs"
    jekyll_env = "development" if serve else "production"

    if use_docker or shutil.which("bundle") is None:
        _require_tool("docker")
        mount = _docker_workspace_path(docs_dir)
        image = "ghcr.io/actions/jekyll-build-pages:latest"
        if serve:
            cmd = (
                f"bundle install && bundle exec jekyll serve --host 0.0.0.0 "
                f"--baseurl {shlex_quote(baseurl)} --livereload"
            )
            port = ["-p", "4000:4000"]
        else:
            cmd = f"bundle install && bundle exec jekyll build --baseurl {shlex_quote(baseurl)}"
            port = []
        _run(
            [
                "docker",
                "run",
                "--rm",
                *port,
                "--entrypoint",
                "/bin/bash",
                "-v",
                f"{mount}:/github/workspace",
                "-w",
                "/github/workspace",
                "-e",
                f"JEKYLL_ENV={jekyll_env}",
                image,
                "-lc",
                cmd,
            ],
            cwd=root,
        )
        if serve:
            print("Jekyll serve running in Docker at http://127.0.0.1:4000" + baseurl + "/", flush=True)
        return

    if serve:
        _run(
            ["bundle", "exec", "jekyll", "serve", "--host", "0.0.0.0", "--baseurl", baseurl, "--livereload"],
            cwd=docs_dir,
            env={"JEKYLL_ENV": jekyll_env},
        )
        return
    _run(
        ["bundle", "exec", "jekyll", "build", "--baseurl", baseurl],
        cwd=docs_dir,
        env={"JEKYLL_ENV": jekyll_env},
    )


def shlex_quote(value: str) -> str:
    if not value:
        return "''"
    if all(c.isalnum() or c in "/_-.:" for c in value):
        return value
    return "'" + value.replace("'", "'\"'\"'") + "'"


def main() -> int:
    parser = argparse.ArgumentParser(description="Build WorkflowForge docs (DocFX + Jekyll).")
    parser.add_argument(
        "--configuration",
        default="Release",
        help="dotnet build configuration (default: Release)",
    )
    parser.add_argument(
        "--baseurl",
        default="/workflow-forge",
        help="Jekyll baseurl, must match GitHub Pages path (default: /workflow-forge)",
    )
    parser.add_argument(
        "--skip-dotnet-build",
        action="store_true",
        help="Skip dotnet build (use when solution is already built)",
    )
    parser.add_argument(
        "--jekyll-only",
        action="store_true",
        help="Only run Jekyll (expects docs/api/ already generated)",
    )
    parser.add_argument(
        "--api-only",
        action="store_true",
        help="Only run dotnet build + DocFX (no Jekyll)",
    )
    parser.add_argument(
        "--serve",
        action="store_true",
        help="Run jekyll serve after building API docs (implies not --api-only)",
    )
    parser.add_argument(
        "--docker-jekyll",
        action="store_true",
        help="Run Jekyll via ghcr.io/actions/jekyll-build-pages (auto if bundle is missing)",
    )
    args = parser.parse_args()

    root = _repo_root()

    if not args.jekyll_only:
        build_api_docs(root, args.configuration, args.skip_dotnet_build)

    if args.api_only:
        return 0

    build_jekyll_site(root, args.baseurl, args.serve, args.docker_jekyll)
    if not args.serve:
        _run_py(root, "append_api_sitemap.py")
        site = root / "docs" / "_site"
        print(f"Site output: {site}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())

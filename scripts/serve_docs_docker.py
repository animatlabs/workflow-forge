#!/usr/bin/env python3
"""
Build docs (DocFX + Jekyll) then serve docs/_site with nginx in Docker for local verification.

Requires: Docker, Python, .NET SDK, Ruby/Bundler (or run build_docs.py --skip-dotnet-build after a prior build).

Example:
  python scripts/serve_docs_docker.py
  python scripts/serve_docs_docker.py --port 8080 --skip-build
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _docker_path(path: Path) -> str:
    posix = path.resolve().as_posix()
    if len(posix) >= 2 and posix[1] == ":":
        return "/" + posix[0].lower() + posix[2:]
    return posix


CONTAINER_NAME = "workflow-forge-docs-preview"


def _stop_containers_on_port(port: int) -> None:
    """Stop Docker containers already bound to the host port (e.g. a prior preview run)."""
    result = subprocess.run(
        ["docker", "ps", "-q", "--filter", f"publish={port}"],
        capture_output=True,
        text=True,
        check=False,
    )
    for cid in result.stdout.split():
        cid = cid.strip()
        if not cid:
            continue
        name = subprocess.run(
            ["docker", "inspect", "-f", "{{.Name}}", cid],
            capture_output=True,
            text=True,
            check=False,
        ).stdout.strip().lstrip("/")
        print(f"Stopping container using port {port}: {name or cid}", flush=True)
        subprocess.run(["docker", "stop", cid], check=False)


def main() -> int:
    parser = argparse.ArgumentParser(description="Build and serve WorkflowForge docs via Docker nginx.")
    parser.add_argument("--port", type=int, default=8080, help="Host port (default: 8080)")
    parser.add_argument("--baseurl", default="/workflow-forge", help="Jekyll baseurl used at build time")
    parser.add_argument(
        "--skip-build",
        action="store_true",
        help="Skip build_docs.py (use existing docs/_site)",
    )
    args = parser.parse_args()

    root = _repo_root()
    site_dir = root / "docs" / "_site"

    if not args.skip_build:
        build_script = root / "scripts" / "build_docs.py"
        subprocess.run(
            [
                sys.executable,
                str(build_script),
                "--baseurl",
                args.baseurl,
                "--docker-jekyll",
            ],
            cwd=root,
            check=True,
        )

    if not site_dir.is_dir() or not any(site_dir.iterdir()):
        print(f"error: site not found or empty: {site_dir}", file=sys.stderr)
        return 1

    base = args.baseurl.strip("/")
    site_mount = _docker_path(site_dir)

    nginx_conf = f"""
worker_processes 1;
events {{ worker_connections 1024; }}
http {{
  include /etc/nginx/mime.types;
  default_type application/octet-stream;
  server {{
    listen 80;
    location /{base}/ {{
      alias /usr/share/nginx/html/;
      try_files $uri $uri/ $uri/index.html =404;
    }}
    location = / {{
      return 302 /{base}/;
    }}
  }}
}}
"""
    with tempfile.NamedTemporaryFile("w", suffix=".conf", delete=False, encoding="utf-8") as tmp:
        tmp.write(nginx_conf)
        conf_path = tmp.name

    conf_mount = _docker_path(Path(conf_path))

    subprocess.run(["docker", "rm", "-f", CONTAINER_NAME], capture_output=True, check=False)
    _stop_containers_on_port(args.port)

    cmd = [
        "docker",
        "run",
        "--rm",
        "--name",
        CONTAINER_NAME,
        "-p",
        f"{args.port}:80",
        "-v",
        f"{site_mount}:/usr/share/nginx/html:ro",
        "-v",
        f"{conf_mount}:/etc/nginx/nginx.conf:ro",
        "nginx:alpine",
    ]
    print(f"Open http://127.0.0.1:{args.port}{args.baseurl}/", flush=True)
    print(f"API: http://127.0.0.1:{args.port}{args.baseurl}/api/WorkflowForge.html", flush=True)
    print(f"+ {' '.join(cmd)}", flush=True)
    try:
        subprocess.run(cmd, check=True)
    except subprocess.CalledProcessError:
        print(
            f"\nerror: could not start nginx on port {args.port}. "
            f"Try another port: python scripts/serve_docs_docker.py --port 8081 --skip-build",
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())

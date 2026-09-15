#!/usr/bin/env python3
"""Unit tests for flatten_docfx_api_urls."""

from __future__ import annotations

import shutil
import tempfile
import unittest
from pathlib import Path

from flatten_docfx_api_urls import _rewrite_text, flatten


class FlattenDocfxApiUrlsShould(unittest.TestCase):
    def test_rewrite_text_strips_api_prefix(self) -> None:
        self.assertIn('href="WorkflowForge.html"', _rewrite_text('href="api/WorkflowForge.html"'))

    def test_rewrite_text_fixes_modern_public_paths(self) -> None:
        out = _rewrite_text('href="../public/docfx.min.css" src="../icon.png"')
        self.assertIn('href="public/docfx.min.css"', out)
        self.assertIn('src="icon.png"', out)

    def test_flatten_moves_nested_html(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            api = Path(tmp) / "api"
            nested = api / "api"
            nested.mkdir(parents=True)
            (nested / "WorkflowForge.html").write_text(
                '<a href="api/Other.html">x</a>', encoding="utf-8"
            )
            flatten(api)
            self.assertTrue((api / "WorkflowForge.html").is_file())
            self.assertFalse((nested / "WorkflowForge.html").exists())
            content = (api / "WorkflowForge.html").read_text(encoding="utf-8")
            self.assertIn('href="Other.html"', content)


if __name__ == "__main__":
    unittest.main()

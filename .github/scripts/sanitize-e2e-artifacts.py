"""Redacta credenciales, cookies y tokens de enlaces en evidencias Playwright."""

from __future__ import annotations

import json
import re
import zipfile
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
ARTIFACT_DIRS = (
    ROOT / "Optica_front" / "playwright-report",
    ROOT / "Optica_front" / "test-results",
)
TEXT_SUFFIXES = {".json", ".trace", ".network", ".html", ".htm", ".md", ".log", ".txt"}
PASSWORD_PATTERN = re.compile(r"E2E-Admin-Acceso-[A-Za-z0-9_-]+!?")
TOKEN_PATTERN = re.compile(
    r"(/api/Reservas/acciones/(?:confirmar|cancelar)/)[A-Za-z0-9_-]+"
)
REDACTED = "[REDACTED]"


def sanitize_string(value: str) -> str:
    value = PASSWORD_PATTERN.sub("[REDACTED-E2E-PASSWORD]", value)
    return TOKEN_PATTERN.sub(r"\1[REDACTED]", value)


def sanitize_value(value: Any, parent_key: str = "") -> Any:
    if isinstance(value, dict):
        sanitized: dict[str, Any] = {}
        for key, item in value.items():
            normalized_key = key.lower()
            if normalized_key in {
                "postdata",
                "postdatabuffer",
                "authorization",
                "cookie",
                "set-cookie",
                "proxy-authorization",
            }:
                sanitized[key] = REDACTED
            elif parent_key == "cookies" and normalized_key == "value":
                sanitized[key] = REDACTED
            elif normalized_key == "headers" and isinstance(item, list):
                sanitized[key] = [sanitize_header(header) for header in item]
            else:
                sanitized[key] = sanitize_value(item, normalized_key)
        return sanitized
    if isinstance(value, list):
        return [sanitize_value(item, parent_key) for item in value]
    if isinstance(value, str):
        return sanitize_string(value)
    return value


def sanitize_header(header: Any) -> Any:
    if not isinstance(header, dict):
        return sanitize_value(header)
    name = str(header.get("name", "")).lower()
    if name in {"authorization", "cookie", "set-cookie", "proxy-authorization"}:
        return {**header, "value": REDACTED}
    return sanitize_value(header)


def sanitize_text(data: bytes) -> bytes:
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError:
        return data

    if not text:
        return data

    try:
        parsed = json.loads(text)
    except json.JSONDecodeError:
        lines = text.splitlines(keepends=True)
        sanitized_lines: list[str] = []
        for line in lines:
            try:
                parsed_line = json.loads(line)
            except json.JSONDecodeError:
                sanitized_lines.append(sanitize_string(line))
            else:
                sanitized_lines.append(
                    json.dumps(sanitize_value(parsed_line), ensure_ascii=False)
                    + ("\n" if line.endswith("\n") else "")
                )
        return "".join(sanitized_lines).encode("utf-8")
    return json.dumps(sanitize_value(parsed), ensure_ascii=False).encode("utf-8")


def sanitize_zip(path: Path) -> int:
    changed = 0
    temporary = path.with_suffix(path.suffix + ".sanitized")
    with zipfile.ZipFile(path, "r") as source, zipfile.ZipFile(
        temporary, "w", compression=zipfile.ZIP_DEFLATED
    ) as target:
        for item in source.infolist():
            data = source.read(item.filename)
            if Path(item.filename).suffix.lower() in TEXT_SUFFIXES:
                data = sanitize_text(data)
                changed += 1
            target.writestr(item, data)
    temporary.replace(path)
    return changed


def main() -> None:
    changed = 0
    for directory in ARTIFACT_DIRS:
        if not directory.exists():
            continue
        for path in directory.rglob("*"):
            if not path.is_file():
                continue
            if path.suffix.lower() == ".zip":
                changed += sanitize_zip(path)
            elif path.suffix.lower() in TEXT_SUFFIXES:
                path.write_bytes(sanitize_text(path.read_bytes()))
                changed += 1
    print(f"Redacted sensitive fields in {changed} E2E evidence files.")


if __name__ == "__main__":
    main()

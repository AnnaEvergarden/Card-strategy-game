# -*- coding: utf-8 -*-
"""扫描 Assets/Scripts 下 C# 文件，生成类与方法列表（Markdown，供 Xmind 使用）。"""
from __future__ import annotations

import re
from pathlib import Path

DOC = Path(__file__).resolve().parent
ROOT = DOC.parent / "Assets" / "Scripts"
OUT = DOC / "2026-03-22_代码结构_Xmind大纲.md"

# 顶层类型声明（不含嵌套）
TYPE_RE = re.compile(
    r"(?:^|\n)\s*(?:\[.*?\]\s*)*(?:(?:public|internal)\s+)?"
    r"(?:abstract\s+|static\s+|sealed\s+|partial\s+)*"
    r"(class|interface|struct|enum)\s+(\w+)"
)


def mask_block_comments(text: str) -> str:
    """将 /* */ 块注释替换为等长空格，避免打乱行号与列号。"""
    return re.sub(r"/\*.*?\*/", lambda m: " " * len(m.group(0)), text, flags=re.DOTALL)


def iter_code_chars_skipping_strings(line: str):
    """遍历一行中参与语法计数的字符（跳过字符串与逐字字符串内的括号）。"""
    i = 0
    n = len(line)
    while i < n:
        if i + 1 < n and line[i : i + 2] == "//":
            return
        c = line[i]
        if i + 1 < n and line[i : i + 2] == '@"':  # verbatim string
            i += 2
            while i < n:
                if line[i] == '"' and i + 1 < n and line[i + 1] == '"':
                    i += 2
                    continue
                if line[i] == '"':
                    i += 1
                    break
                i += 1
            continue
        if c == '"':
            i += 1
            while i < n:
                if line[i] == "\\" and i + 1 < n:
                    i += 2
                    continue
                if line[i] == '"':
                    i += 1
                    break
                i += 1
            continue
        if c == "'":
            i += 1
            while i < n:
                if line[i] == "\\" and i + 1 < n:
                    i += 2
                    continue
                if line[i] == "'":
                    i += 1
                    break
                i += 1
            continue
        yield c
        i += 1


BAD_NAMES = frozenset(
    "if for foreach while switch catch using lock fixed get set add remove".split()
)


def extract_member_sig(line: str) -> str | None:
    s = line.strip()
    if "//" in s:
        s = s.split("//", 1)[0].strip()
    if "(" not in s or ")" not in s:
        return None
    if not re.match(r"^(public|protected|private|internal)\s+", s):
        return None
    if "delegate " in s:
        return None
    if re.search(r"\b(class|interface|struct|enum)\s+\w+", s):
        return None
    idx = s.find("(")
    before = s[:idx].strip()
    parts = before.split()
    if len(parts) < 2:
        return None
    name = parts[-1]
    if name in BAD_NAMES:
        return None
    return s


def parse_file(path: Path) -> list[tuple[str, list[str]]]:
    """[(类型名, [方法/属性/事件签名行]), ...] 按文件中声明顺序。"""
    raw = mask_block_comments(path.read_text(encoding="utf-8"))

    depth = 0
    pending_type: tuple[str, str] | None = None
    type_stack: list[tuple[str, int]] = []
    members: dict[str, list[str]] = {}
    current_type: str | None = None

    for line in raw.splitlines():
        # 类型声明
        lm = TYPE_RE.search(line)
        if lm:
            pending_type = (lm.group(1), lm.group(2))

        # 成员（用原始行，含 public ...）
        sig = extract_member_sig(line)
        if sig and current_type:
            if current_type not in members:
                members[current_type] = []
            if sig not in members[current_type]:
                members[current_type].append(sig)

        # 括号：仅在本行代码部分（去掉 // 之后）且跳过字符串
        code = line.split("//", 1)[0]
        for c in iter_code_chars_skipping_strings(code):
            if c == "{":
                depth += 1
                if pending_type:
                    _, tname = pending_type
                    type_stack.append((tname, depth))
                    current_type = tname
                    if tname not in members:
                        members[tname] = []
                    pending_type = None
            elif c == "}":
                depth -= 1
                while type_stack and depth < type_stack[-1][1]:
                    type_stack.pop()
                if type_stack:
                    current_type = type_stack[-1][0]
                else:
                    current_type = None

    order: list[str] = []
    seen: set[str] = set()
    for line in raw.splitlines():
        lm = TYPE_RE.search(line)
        if lm:
            name = lm.group(2)
            if name not in seen:
                seen.add(name)
                order.append(name)

    out: list[tuple[str, list[str]]] = []
    for name in order:
        if name in members:
            out.append((name, members[name]))
    return out


def folder_tree() -> str:
    lines = []
    for p in sorted(ROOT.rglob("*.cs")):
        lines.append(p.relative_to(ROOT).as_posix())
    return "\n".join(lines)


def main() -> None:
    if not ROOT.exists():
        print("Missing", ROOT)
        return

    md: list[str] = []
    md.append("# 代码结构与方法索引（Xmind 用）")
    md.append("")
    md.append("> 由 `Documentation/_gen_cs_outline.py` 从 `Assets/Scripts` 自动生成。")
    md.append("> **导入 Xmind**：用「大纲」视图粘贴下面「缩进大纲」一节；或按「文件夹 → .cs 文件 → 类 → 方法」手动建图。")
    md.append("")
    md.append("## 1. 文件目录树（Scripts 下相对路径）")
    md.append("")
    md.append("```")
    md.append(folder_tree())
    md.append("```")
    md.append("")

    md.append("## 2. 按文件：类与成员")
    md.append("")
    md.append("下列「成员」含：`public` / `protected` / `private` / `internal` 的方法、构造、属性（单行形式）、事件（若匹配到）。")
    md.append("")

    all_cs = sorted(ROOT.rglob("*.cs"))
    for fp in all_cs:
        rel = fp.relative_to(ROOT.parent.parent / "Assets").as_posix()
        md.append(f"### `{rel}`")
        md.append("")
        try:
            blocks = parse_file(fp)
        except Exception as e:
            md.append(f"*解析失败: {e}*")
            md.append("")
            continue
        if not blocks:
            md.append("*（未解析到类型或为空）*")
            md.append("")
            continue
        for cls, mems in blocks:
            md.append(f"- **{cls}**")
            if not mems:
                md.append("  - （无匹配成员行，或仅含字段等）")
            else:
                for m in mems:
                    md.append(f"  - `{m}`")
            md.append("")

    md.append("## 3. 缩进大纲（可整段复制到 Xmind 大纲）")
    md.append("")
    for fp in all_cs:
        rel = fp.relative_to(ROOT).as_posix()
        md.append(f"- Scripts/{rel}")
        try:
            blocks = parse_file(fp)
        except Exception:
            continue
        for cls, mems in blocks:
            md.append(f"  - {cls}")
            for m in mems:
                short = m.strip()
                if len(short) > 120:
                    short = short[:117] + "..."
                md.append(f"    - {short}")

    OUT.write_text("\n".join(md), encoding="utf-8")
    print("OK", OUT)


if __name__ == "__main__":
    main()

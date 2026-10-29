---
name: doc-writing
description: Rules for writing and editing Korean docs the user reads in Project_T (Docs/기획서, system docs, 결정 사항, 아이디어, the 컨셉 section of CLAUDE.md). Use when creating or changing any of these, including a new system doc under Docs/기획서/시스템.
---

# Doc writing

Docs the user reads are written in Korean. This file is in English because Claude reads it.
English docs Claude writes for itself (`Docs/레퍼런스/Reference *.md`, `Docs/인수인계 문서.md`) are out of scope.

## Why

The user reads and edits these docs in Obsidian, and wants to take in a section at a glance.
These rules come from the user's own edits to `Docs/기획서/시스템/죽음 시스템.md`, the reference example.

## Style

- Terse endings: `~함`, `~됨`, `~음`, or end on a noun (`위험한 것을 조사.`). Not `~한다`.
- One fact per bullet. Put reasons and exceptions on an indented line below.
	```
	- 시체 파일은 창 닫기 죽음에서만 생김.
		- 게임 안의 죽음에서는 생기지 않음.
	```
- A blank line between top-level bullets.
- Split by category with headings rather than listing cases in a sentence (`## 죽음 트리거` → `### 인게임` / `### 메타픽션`).
- Examples go on an indented line starting with `Ex.`.
- Use exact terms: `Yarn 노드`, not `노드`. Keep English technical terms as they are (`Room`, `Edit Test`). Short forms are fine (`Pb` for prefab).
- Headings say what the section is for (`확인 필요`, not `빈 곳`).
- Never say the same thing twice in one doc. If a table and a diagram overlap, cut one.

## Obsidian formatting

- Each `#` heading is followed by a `---` line.
- Leave a blank line between `---` and a table, or the table will not render.
- Break long table cells with `<br>`.
- Diagrams use ```` ```mermaid ````.
- Link other docs with `[[file name]]`, no path. Put the link where the text already mentions that system (`→ [[세션 시스템]]` at the end of a table cell or bullet), not in a separate "related" section.
- Linking to a doc that does not exist yet is fine. It shows as an unresolved link and marks a doc still to write.
- Do not rename doc files. If one must be renamed, update every `[[link]]` to it in the same change.

## System docs

- One file per system in `Docs/기획서/시스템/`, named `{시스템} 시스템.md`.
- Sections in this order:
	1. Overview (untitled, under the `#` title): what the system does, a few bullets.
	2. `# 구성`: a table of scripts and what each does. No prefab table unless the user asks.
	3. `# 흐름`: one mermaid `sequenceDiagram` between the scripts, plus bullets for paths that skip it.
	4. `# 쓰는 법`: numbered steps per task (making a trigger, setting up what remains, etc.). Include the calls other systems use (`DialogueService.Play`, `InputLock.Lock`), because a later feature reads this section to find the existing API before building on it.
	5. `# 확인 필요`: what is missing or half-done.
- Only state what the code shows. If unsure, put it under `확인 필요` instead of guessing.

## New doc types

- A new kind of doc (UI docs, etc.) gets its own `## {Kind} docs` section in this file, not a new skill.
- Write the first one, let the user edit it, then write the section from their edits, the same way `## System docs` was made.

## Editing

- Apply these rules to text you write or change. Do not rewrite the user's untouched text.
- Edit Korean text with the Edit tool. Do not bulk-replace Korean with Perl (it breaks UTF-8) or with Python text mode on Windows (it turns LF into CRLF).

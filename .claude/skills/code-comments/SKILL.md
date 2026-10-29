---
name: code-comments
description: Rules for writing and editing C# code comments in Project_T. Use when adding, changing, or reviewing comments in Assets/Scripts, or when writing a system doc that quotes them.
---

# Code comments

Comments are written in Korean. This file is in English because Claude reads it.

## Why

The code is mostly written by Claude and read by the user, and Claude itself starts each session as a first-time reader. A comment must let a new reader understand a unit without reading every line.

## What to write

- **Summary comment on every class and every non-trivial method.** One or two lines saying what the unit does as a whole. For a flow, give the steps: `A → B → C`.
- **Say who calls it** when the caller is not obvious from the file (for example, `internal` entry points and static state).
- **Line comments only for "why" and for contracts that break if ignored.** Examples: ordering reasons, "do not call from Awake", Unity behavior that surprises (a stopped coroutine does not run `finally`).
- **Explain non-obvious call styles** such as `.Forget()` or driving an `IEnumerator` by hand with `MoveNext`.

## What not to write

- A comment that restates one line of code.
- History ("previously this did X"). That belongs in git.
- A guess at intent. If the reason is unknown, leave the comment out rather than invent one. A wrong comment is worse than none.
- Units or facts already clear from the name (`RoomMoveTimeout`).

## Style

- Terse noun/verb endings: `~함`, `~됨`, `~음`. Not `~한다`.
- One sentence per line. Split a long one across lines.
- Use `~` for ranges: `사망 시작 ~ 리스폰 진입`.
- Keep English technical terms as they are (`Room`, `Edit Test`, class and method names).
- This applies to comments you write or edit. Do not rewrite untouched comments.
- `Debug.Log` messages and exception text are not comments. Leave their wording as is.

## Fixing old comments

- A comment that no longer matches the code: fix it, and say so.
- Check names it mentions (classes, scenes) still exist before keeping it.
- Do not change code behavior while editing comments.

## Checking

To test whether comments help, spawn a fresh agent (Sonnet) that reads only the one file, with no other files or docs. Ask it to explain the flow, then list what it could not understand or found redundant. Treat the score as one sample, not a verdict.

The reference example is `Assets/Scripts/Death/DeathSequence.cs`.

# 컨셉
---
- 한 문장: 플레이어가 소녀를 바라보는 게임.

- 장르: 탑다운 어드벤처.

- 보고 싶은 장면: 한 소녀가 망가지는 것.

- 테마의 뿌리: 바라보다, 죽음.
	- 메타픽션은 '바라보다'가 드러나는 방식.

- 플레이어의 경험
	- 이 소녀를 좋아하게 됨.
	- 이 소녀가 실존한다고 믿음.


# 기획할 때
---
## 판단 기준

- 게임플레이를 하나 해 볼 때마다 네 가지를 묻는다.
	1. 탑다운 어드벤처에서 돌아가는가?
	2. 소녀가 망가지는 장면이 나오는가?
	3. 메타(창 닫기, 시체 파일, 플레이어를 부르는 것)와 이어지는가?
	4. 해 보고 나서 소녀가 더 좋아지는가, 더 진짜처럼 느껴지는가?
- 2번과 4번 중 하나라도 확 오면 남긴다. 둘 다 안 오면 다른 걸 해 본다.

## 원칙

- 장소, 기믹, 긴장 장치는 테마의 뿌리에서 뻗어 나와야 한다. 뿌리 없이 갑자기 나온 것(숲, 주사위)은 애매해진다.
- 장면 하나, 문제 하나씩 붙잡는다. 회차 구조 같은 큰 틀로 건너뛰지 않는다.
- 장소나 설정보다 장면(대사)을 먼저 써 본다.
- 소녀가 자기 뜻을 보이는 순간이 통한다(외면, 시체를 보는 반응). 소녀가 도구가 되면 안 통한다(문양 퍼즐).
- 레퍼런스의 장르 틀이 보이면 탈락시킨다. 사용자는 독자적인 것을 하고 싶어 한다.


# Current state

- The concept restarted from scratch on 2026-10-28.
- The old world and design docs live on the `archive/saint-scenario` branch. Do not use them as premises; open them only when needed.
- Existing code and assets (events, save, dialogue, additive scenes) stay in use.
- Goal: build one region (save point A → B) by the end of November 2026.
- The current task is not kept here. Across account switches it is carried by `Docs/인수인계 문서.md`.


# Reference docs

- `Docs/레퍼런스/Reference Game.md`: the seven reference games (OneShot, Undertale, Ib, Doki Doki Literature Club, Depersonalization, Black Souls 1·2). Read first when discussing concept or scenes.
- `Docs/레퍼런스/Reference Map Design.md`: map structure notes on OneShot, OMORI, Undertale. Read first when building a graybox map.
- `Docs/기획서/아이디어.md`: short notes on ideas. Not decided.
- `Docs/기획서/결정 사항.md`: what is decided (including small calls like camera size) and what was dropped. Add one line whenever something is decided.
- `Docs/기획서/시스템/`: one doc per code system (death, interaction, dialogue, save, Room, session, player). The `확인 필요` section at the end of each lists what is missing and the constraints.
- `Docs/인수인계 문서.md`: handoff between accounts. Exists only right after an account switch.
	- Written in English just before switching accounts.
	- Holds the current task and open questions. Does not repeat CLAUDE.md; if they disagree, CLAUDE.md wins.
	- The first session after the switch reads it, then deletes it in that day's `chore` commit. Forks do not need it.
	- If it does not exist, there is no pending handoff.
- Language: docs the user reads and edits are Korean and follow the `doc-writing` skill. Docs only Claude reads (`Reference *.md`, the handoff) are English.


# Workflow

## Before building a feature

- First find the existing systems the feature depends on. (Ex. shop → inventory, dialogue, save)
- Check them in the system docs (`Docs/기획서/시스템/`).
	- `구성`, `쓰는 법`: the existing API.
	- `확인 필요`: constraints and what is not done.
- If a doc is missing or disagrees with the code, read the code and fix the doc first.
- Before building, tell the user which of three it is:
	- Use the existing API as is.
	- Add an API to an existing system.
	- Refactor an existing system.
- Do not build on plans that `결정 사항` marks as on hold.

## Branches

- Simple changes (docs, small fixes, value tweaks) go on main with no branch.
- Features start from main as `feature/{name}`. One feature = one issue = one branch = one PR to main.
	- End commit messages with `(#number)`.
	- PR body: `Closes #number` if the feature is done, `관련 #number` if partial.
- Cleanup or refactoring big enough to need review starts from main as `refactor/{name}`.

## Commits

- Messages are Korean, in `type: content` form. (Ex. `feat: ...`, `fix: ...`, `docs: ...`)
- Do not commit after every edit.
- Simple changes are batched into one `chore:` commit a day on main.
	- Commit when the user says to wrap up the day. Claude cannot tell when the day ends.
	- Message: `chore: MM-DD 정리`, with one body line per change.

## Code comments

- Follow the `code-comments` skill when writing or changing comments.

## Docs

- Follow the `doc-writing` skill when writing or changing Korean docs the user reads.

## Unity MCP approval

- OK without asking: creating and adding GameObjects and components, changing properties (position, values, etc.).
- Always ask first: anything that deletes or removes (deleting GameObjects, removing components, deleting or switching scenes) and bulk operations.
- Same for the main session and subagents.

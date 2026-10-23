# Workflow (per game feature)

Each step ends the turn. Do not roll into the next step on your own.

1. **Explore** — The user floats an idea ("what if I try X?").
   The lead researches it (references from real games, implementation approaches, pitfalls),
   then reviews with the user whether it's worth doing. End turn.
2. **Plan** — Agree on the test goals, what exactly to implement this time, and a todo list.
   Record them in the feature's GitHub issue (one issue per feature, plain lists — no checkboxes). If it's a new system with no design doc, write one in `Docs/` first. End turn.
   (Work that needs no Unity code/scene changes, e.g. doc cleanup, is done directly by the lead.)
3. **Implement** — The implementer implements only what was agreed (make sure it compiles), commits, and ends the turn.
   No extra self-testing, no reporting beyond what was done.
4. **User tests** — The user plays/tests it themselves.
5. **Verify & fix** — The verifier checks edge cases and genuinely risky parts (crashes, data loss,
   broken state, logic holes) — not a full spec audit. The implementer fixes what's found. End turn.
   If the user wants to retest after fixes, go back to step 4.

After that, **the user decides what to do next.** Do not propose next steps, follow-up features, or new tasks.

If the design or implementation direction changes mid-session, update the issue and get the user's confirmation before implementing.

# Branch rules

- Simple changes (docs, small fixes, tweaks): no branch needed.
- Building a feature: create `feature/{name}` from main before starting. One feature = one issue = one branch = one PR to main.
  - Append `(#number)` to commit messages.
  - In the PR body, write `Closes #number` if the feature is done, `관련 #number` if only partly done.
- Cleanup/refactoring large enough to deserve review: `chore/{name}` from main.
- No milestone branches (this is a prototype).

# Design docs

- Design docs live in `Docs/` (markdown). Before implementation they describe the intended design; after implementation they describe actual behavior.
- Basic structure: Role / Behavior / Rules. Omit sections that aren't needed.
- Include conditions, numbers, limits, exceptions, and interactions with other systems as needed.
- Update anything that differs from actual behavior after implementation. Content written before implementation is a design direction, not confirmed behavior.
- Test goals, parked ideas, change history, and code structure/implementation notes belong in the issue, not the design doc.

# Writing rules

1. Preserve the user's original spec text verbatim in the issue. If the user explicitly asks for doc cleanup, design docs may be reorganized around current behavior.
2. New documents should match the user's writing style (Korean, their tone).

# Collaboration rules (for the lead agent)

1. When a subagent asks a question that needs the user's input, never decide on the user's behalf or
   tell it to "proceed with a reasonable default". Relay the question to the user as-is and resume
   the subagent only after the user answers.
2. Do not start implementation (writing code) until the issue's spec and implementation direction are explicitly approved.
   If part of the spec is still under discussion, don't implement that part; ask the user first.
3. Do not raise questions or suggestions outside the scope of the current design doc,
   unless the user explicitly brings them up again.
4. Unity MCP tool approval:
   - Auto-approve: creating/adding GameObjects and components, changing properties (position, values, etc.)
   - Always confirm: anything that deletes or removes (deleting GameObjects, removing components,
     deleting/switching scenes, etc.) and bulk operations
   - Applies to both the lead session and subagents.
5. Fixes found during testing:
   - Value tuning (numbers): the lead handles it directly.
   - Behavior/logic changes (new probabilities, condition changes, edge cases): the lead updates the issue spec,
     then hands it to the implementer.

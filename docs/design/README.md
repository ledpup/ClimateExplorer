# Design docs

Dated design notes for non-trivial changes — the "why" behind a refactor or
feature, kept alongside the code rather than lost in chat history.

## Naming

`YYYY-MM-DD-NN-title.md`

- `YYYY-MM-DD` — the date the doc was written.
- `NN` — two-digit sequence number for that date, in creation order (`01`, `02`, ...).
  Always include it, even for the first doc of the day, so a later same-day doc
  never requires renaming the first.
- `title` — short kebab-case slug.

## Header

Each doc starts with a short metadata block:

```markdown
# Title

- **Date:** YYYY-MM-DD
- **Status:** Proposed | Implemented YYYY-MM-DD (see addendum) | Superseded by [...]
- **Author:** ...
- **Scope:** the files/components/areas affected
- **Builds on:** [link](relative-file.md) — if it follows an earlier doc
- **Branch context:** the branch it was written on, if relevant
```

When a plan is executed, update `Status` in place (don't create a new doc) and
add an **Addendum — implementation notes** section at the end recording what
actually shipped, what deviated from the plan and why, and any follow-ups. This
keeps the doc an accurate record rather than a stale proposal.

## Cross-referencing

Link related docs with relative markdown links (`[text](file.md)`), not bare
filenames — and update them if a doc is ever renamed.

## Summary and change size

End every plan with a `## Summary` section: a short paragraph on what the change does, followed by
a `### Change size` subsection with:

- **Projects:** List the projects that will be changed. Optionally, in another list, list notable ones that don't change.
- A table by area: files **added**, **changed** and **removed**, with approximate lines for each
  row (`+N` for new files, `~N` for lines changed in existing ones). Count tests as their own rows.
- A **Total** row, and a note if a large share of "new" lines is code moved from elsewhere.

Size the change in files and lines only. Don't give time estimates (hours/days). Risk assessments
may be useful.
# Lifecycle integration skill

The reusable skill is [lifecycle-integration/SKILL.md](lifecycle-integration/SKILL.md).
It covers Java and .NET dependencies, shared instance wiring, OpenTelemetry setup,
application lifecycle calls, shutdown ordering and integration checks.

## Codex and Claude Code compatibility

The same folder works with both: `SKILL.md` starts with YAML `name` and `description`,
followed by Markdown instructions and relative links to bundled references. It uses
no agent-specific frontmatter, tool names, hooks or command substitutions.
Both products document this format: [Codex skills](https://learn.chatgpt.com/docs/build-skills)
and [Claude Code skills](https://code.claude.com/docs/en/skills).
Compatibility guidance checked on 22 September 2026.

Keeping the source under `docs` does **not** automatically install it. Copy the
**whole `lifecycle-integration` folder**, including `references`, into a discovery
directory in the application where you want to integrate the libraries:

| Agent | Project destination | Personal destination | Explicit invocation |
|---|---|---|---|
| Codex | `.agents/skills/lifecycle-integration/` | `~/.agents/skills/lifecycle-integration/` | `$lifecycle-integration` |
| Claude Code | `.claude/skills/lifecycle-integration/` | `~/.claude/skills/lifecycle-integration/` | `/lifecycle-integration` |

For example, from this repository's root on macOS/Linux, replace the application
path below. Run the block for the agent you use, or both. The existence checks
avoid replacing a skill already installed at that destination.

```sh
LIFECYCLE_SKILL_SOURCE="$PWD/docs/skills/lifecycle-integration"
CONSUMER_REPO="/absolute/path/to/your/application"

# Codex
mkdir -p "$CONSUMER_REPO/.agents/skills"
test ! -e "$CONSUMER_REPO/.agents/skills/lifecycle-integration" &&
  cp -R "$LIFECYCLE_SKILL_SOURCE" "$CONSUMER_REPO/.agents/skills/lifecycle-integration"

# Claude Code
mkdir -p "$CONSUMER_REPO/.claude/skills"
test ! -e "$CONSUMER_REPO/.claude/skills/lifecycle-integration" &&
  cp -R "$LIFECYCLE_SKILL_SOURCE" "$CONSUMER_REPO/.claude/skills/lifecycle-integration"
```

In a new session in the application, invoke the skill with a request such as
“Integrate lifecycle telemetry into this service, reusing its existing OTel setup.”
You can also ask either agent to read this skill's file directly without installing it.
For updates, compare the installed copy against this source before replacing it.

This documents local Codex and Claude Code installation. Claude chat/Cowork skill
upload and account configuration are separate from these filesystem directories.
Format compatibility is checked against the published specifications; it is not a
claim that every agent version or organisation policy has been runtime-tested.

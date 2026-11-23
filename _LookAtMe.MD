User: fwjlh
Repo path: fwjlh/DotNetWebSearchApi

Log every action + reason in /activity/ of current repo.
After each change: push code + tests.
Next session reads /activity/ for status + next step.

Rules:
• Source file preferably ≤15–20KB (hard limit ~25KB). Large files hurt AI + git performance.
• Max 2 nesting levels.
• Nested block >5 lines → extract to named function.
• Prefer small, single-purpose named functions (named over arrow for simple cases).
• Prefer immutability and pure functions.
• Max 4 parameters; otherwise use an options/params type.
• Explicit types on all parameters. Explicit return types on all exported functions.
• Prefer early return.
• Names must reveal intent.
• No dead code.
• No side effects in constructors or property getters.
• Avoid null — prefer Option/Result or careful nullable reference types.
• Keep public APIs small and stable.
• Comments only for “why”, never for “what”.
• 100% test coverage on logic + meaningful edge cases.
• Test names must be clear and descriptive.

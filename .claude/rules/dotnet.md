---
paths:
  - "**/*.cs"
---

# Writing C# in Creole

Full rules: CONTRIBUTING.md § Code. Style that `.editorconfig` enforces at build time is not repeated here.

- Runtime libraries (`src/Creole`, `src/Creole.Testing`) target `net11.0` only and must stay Native AOT and trim safe (G8): no `Reflection.Emit`, no reflection or `dynamic` on the hot path, no `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` on public API. IL2xxx and IL3xxx warnings are errors.
- `src/Creole.Generators` targets `netstandard2.0` because Roslyn loads it there. It cannot use `net11.0` APIs or reference `Creole`. Generators are incremental (`IIncrementalGenerator`); pipeline values must be equatable.
- Warnings are errors. Fix the cause. Suppress only with a justification comment, as locally as possible.
- Every public type and member has XML docs. A public API change updates `docs/` in the same change.
- `<summary>` tags go on their own lines (`/// <summary>`, the text, `/// </summary>`), even for one-line summaries. Other XML doc tags may stay inline.
- The whitepaper's code samples are design sketches. Do not copy a signature from it as settled API unless a card or decision settles it.
- Tests pin semantics the whitepaper states, not implementation details. Test projects use xUnit v3 on Microsoft.Testing.Platform.
- Diagnostics use the `CRL` prefix. IDs are public contract and each gets `docs/reference/diagnostics/CRLnnnn.md` when it is introduced.
- New package versions go in `Directory.Packages.props`, never in a project file. Keep `Microsoft.CodeAnalysis.*` at or below the compiler in the pinned SDK.

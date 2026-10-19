---
paths:
  - "**/*.cs"
---

# Writing C# in Creole

Full rules: CONTRIBUTING.md § Code. Style that `.editorconfig` enforces at build time is not repeated here.

- Runtime libraries (`src/Creole`, `src/Creole.Testing`) target `net11.0` only and must stay Native AOT and trim safe (G8): no `Reflection.Emit`, no reflection or `dynamic` on the hot path, no `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` on public API. IL2xxx and IL3xxx warnings are errors.
- `src/Creole.Generators` targets `netstandard2.0` because Roslyn loads it there. It cannot use `net11.0` APIs or reference `Creole`. Generators are incremental (`IIncrementalGenerator`); pipeline values must be equatable.
- Warnings are errors. Fix the cause. Suppress only with a justification comment, as locally as possible.
- One type per file. Generic types that share a name and differ only in arity go in one file, in order of arity. A helper used only in that file is a `file` type after the main type, not a nested type; nest only when a `file` type cannot work (part of the containing type's API, in one of its member signatures, or used from another file).
- Every file in a project uses the project's namespace (`Creole`, `Creole.Testing`, `Creole.Tests`, `Creole.Benchmarks`, …), whatever folder it is in.
- `sealed` is optional on private, internal and `file` types. Node and plan classes stay `sealed` (ADR-0001).
- Member order: by kind (nested classes, structs, records, other nested types; constants; fields; properties and indexers; events; constructors; methods; operators), then accessibility (private first, public last), then name ignoring case and a leading `_`. Exception: a field or property comes after any member its initializer reads (CRL0004). Details in CONTRIBUTING.md § Conventions.
- A combinator null-checks each parser argument, and an extension member its receiver too, with a `[CallerArgumentExpression]` parameter passed to `Guard.NotNull`. `ExtensionReceiverTests` checks the receivers. An instance method cannot check its receiver (C# throws `NullReferenceException` first); CRL0004 covers it.
- Every public type and member has XML docs. A public API change updates `docs/` in the same change.
- `<summary>` tags go on their own lines (`/// <summary>`, the text, `/// </summary>`), even for one-line summaries. Other XML doc tags may stay inline.
- Break XML doc lines only at natural breaks (end of a sentence, a colon, a semicolon), never at a column width.
- The whitepaper's code samples are design sketches. Do not copy a signature from it as settled API unless a card or decision settles it.
- Tests pin semantics the whitepaper states, not implementation details. Test projects use xUnit v3 on Microsoft.Testing.Platform.
- Diagnostics use the `CRL` prefix. IDs are public contract and each gets `docs/reference/diagnostics/CRLnnnn.md` when it is introduced.
- New package versions go in `Directory.Packages.props`, never in a project file. Keep `Microsoft.CodeAnalysis.*` at or below the compiler in the pinned SDK.

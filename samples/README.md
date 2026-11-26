# Samples

Runnable sample grammars. Each sample is its own project under this folder, added to `Creole.slnx` when it is written. The first samples arrive with the worked examples in the [whitepaper](../whitepaper.md#worked-examples).

| Sample | What it shows | Run it |
| --- | --- | --- |
| [Creole.Samples.Indentation](Creole.Samples.Indentation/) | [Example B](../whitepaper.md#b-an-indentation-sensitive-language-in-three-layers): an indentation-sensitive language in three layers. A lexer, a layout pass that turns indentation into INDENT and DEDENT tokens with [layer state](../docs/concepts/layer-state.md), and a parser. Prints the program in a standard form, or its errors. | `dotnet run --project samples/Creole.Samples.Indentation -- <file>` |

`tests/Creole.Tests` references each sample and tests it.

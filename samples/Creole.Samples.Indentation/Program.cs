using Creole;
using Creole.Samples.Indentation;

if (args is not [var path])
{
    Console.Error.WriteLine("Usage: Creole.Samples.Indentation <file>");
    return 2;
}

var mini = Pipeline.From(MiniLexer.Lexer).Then(Layout.Pass).Then(MiniGrammar.Root);
var result = mini.Parse(File.ReadAllText(path), source: path);

if (result is Parsed<Script> parsed)
{
    Console.Write(parsed.Value);
    return 0;
}

Console.Error.WriteLine(result.Diagnostics.Render());
return 1;

using System.Security.Cryptography;
using System.Text;
using Creole.Benchmarks.Arithmetic;
using Creole.Benchmarks.Inputs;
using Creole.Benchmarks.Json;

namespace Creole.Benchmarks.Tests;

public class BaselineAgreementTests
{
    private static readonly string[] _jsonParsers = ["Parlot", "ParlotCompiled", "Pidgin", "Superpower", "Sprache"];

    public static TheoryData<string, string> JsonCases =>
        [.. from input in JsonInputs.Names from parser in _jsonParsers select (input, parser)];

    public static TheoryData<string> ArithmeticParsers =>
        ["Parlot", "ParlotCompiled", "Pidgin", "PidginFolds"];

    [Theory]
    [MemberData(nameof(JsonCases))]
    public void JsonBaselineMatchesSystemTextJson(string input, string parser)
    {
        var text = JsonInputs.Get(input);
        var expected = SystemTextJsonBaseline.Parse(Encoding.UTF8.GetBytes(text)).ToCanonicalString();
        var actual = ParseJson(parser, text).ToCanonicalString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ArithmeticParsers))]
    public void ArithmeticBaselineMatchesHandWritten(string parser)
    {
        foreach (var expression in ArithmeticInputs.Expressions)
        {
            var expected = HandWrittenArithmetic.Evaluate(expression);
            var actual = EvaluateArithmetic(parser, expression);

            Assert.True(expected.Equals(actual), $"{parser} gave {actual} for '{expression}'; expected {expected}.");
        }
    }

    [Fact]
    public void HandWrittenArithmeticFollowsPrecedenceAndAssociativity()
    {
        Assert.Equal(7, HandWrittenArithmetic.Evaluate("1 + 2 * 3"));
        Assert.Equal(-4, HandWrittenArithmetic.Evaluate("1 - 2 - 3"));
        Assert.Equal(2, HandWrittenArithmetic.Evaluate("8 / 2 / 2"));
        Assert.Equal(-9, HandWrittenArithmetic.Evaluate("-(1 + 2) * 3"));
    }

    [Theory]
    [InlineData(JsonInputs.Small, "7d587797111b389913b8b19aca577430a6f47c99e631760f5f879e53a699776f")]
    [InlineData(JsonInputs.Medium, "aad6cfbc496f5077d4782aeb422ea73c8ed0301026583f59b8cb890b8c9715d1")]
    [InlineData(JsonInputs.Large, "a70aa2d0928838b84808be699f09109e0a2106978f0c3ee80db76b2374f314ef")]
    public void JsonInputIsUnchanged(string input, string sha256) =>
        Assert.Equal(sha256, Hash(JsonInputs.Get(input)));

    [Fact]
    public void ArithmeticInputIsUnchanged() =>
        Assert.Equal("8c4f7b4683e2e77ef95a50313c8d747ac0527228f98ffb99de73daf49e4be681", Hash(string.Join('\n', ArithmeticInputs.Expressions)));

    private static JsonValue ParseJson(string parser, string text) => parser switch
    {
        "Parlot" => ParlotJson.Parse(ParlotJson.Interpreted, text),
        "ParlotCompiled" => ParlotJson.Parse(ParlotJson.Compiled, text),
        "Pidgin" => PidginJson.Parse(text),
        "Superpower" => SuperpowerJson.Parse(text),
        "Sprache" => SpracheJson.Parse(text),
        _ => throw new ArgumentOutOfRangeException(nameof(parser), parser, null),
    };

    private static double EvaluateArithmetic(string parser, string expression) => parser switch
    {
        "Parlot" => ParlotArithmetic.Evaluate(ParlotArithmetic.Interpreted, expression),
        "ParlotCompiled" => ParlotArithmetic.Evaluate(ParlotArithmetic.Compiled, expression),
        "Pidgin" => PidginArithmetic.Evaluate(PidginArithmetic.OperatorTable, expression),
        "PidginFolds" => PidginArithmetic.Evaluate(PidginArithmetic.Folds, expression),
        _ => throw new ArgumentOutOfRangeException(nameof(parser), parser, null),
    };

    private static string Hash(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

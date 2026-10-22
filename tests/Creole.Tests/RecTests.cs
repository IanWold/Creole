using System.Globalization;

using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum J { LBrace, RBrace, LBracket, RBracket, Colon, Comma, String, Number, True, False, Null }

file readonly record struct JTok(J Kind, string Text) : IKinded<J>;

file abstract record JsonValue;

file sealed record JsonString(string Value) : JsonValue;

file sealed record JsonNumber(double Value) : JsonValue;

file sealed record JsonBool(bool Value) : JsonValue;

file sealed record JsonNull : JsonValue;

file sealed record JsonArray(IReadOnlyList<JsonValue> Items) : JsonValue;

file sealed record JsonObject(IReadOnlyList<KeyValuePair<string, JsonValue>> Members) : JsonValue;

file sealed class JsonGrammar : TokenGrammar<JTok, J>
{
#pragma warning disable CS8604
    public static readonly Parser<JTok, JsonValue> Value = Rec(() => OneOf(
        Kind(J.String).Select<JsonValue>(static t => new JsonString(t.Text)),
        Kind(J.Number).Select<JsonValue>(static t => new JsonNumber(double.Parse(t.Text, CultureInfo.InvariantCulture))),
        Kind(J.True).As<JsonValue>(new JsonBool(true)),
        Kind(J.False).As<JsonValue>(new JsonBool(false)),
        Kind(J.Null).As<JsonValue>(new JsonNull()),
        ArrayValue,
        ObjectValue
    ));
#pragma warning restore CS8604

    public static readonly Parser<JTok, JsonValue> ArrayValue =
        Value.SepBy(Kind(J.Comma))
             .Between(Kind(J.LBracket), Kind(J.RBracket))
             .Select<JsonValue>(static items => new JsonArray(items));

    public static readonly Parser<JTok, JsonValue> ObjectValue =
        Seq(Kind(J.String), Kind(J.Colon), Value)
            .Select(static (k, _, v) => KeyValuePair.Create(k.Text, v))
            .SepBy(Kind(J.Comma))
            .Between(Kind(J.LBrace), Kind(J.RBrace))
            .Select<JsonValue>(static members => new JsonObject(members));

    public static readonly Parser<JTok, JsonValue> Document =
        Grammar.Deterministic(Value.Before(End));
}

file sealed class Forward : CharGrammar
{
    public static readonly Parser<char, int> First = Rec(() => Second);

    public static readonly Parser<char, int> Second = Return(2);
}

file sealed class Lists : CharGrammar
{
    private static readonly Parser<char, int> _item = Rec(() => List);

    public static readonly Parser<char, int> List =
        _item.SepBy(Char(',')).Between(Char('['), Char(']')).Select(static items => items.Count);
}

file sealed class Parens : CharGrammar
{
    public static readonly Parser<char, int> Group = Rec(() => Inner).Between(Char('('), Char(')')).Select(static depth => depth + 1);

    public static readonly Parser<char, int> Inner = OneOf(Group, Char('x').As(0));
}

file sealed class Names : CharGrammar
{
    public static readonly Parser<char, int> Value = Rec(() => OneOf(Return(1), Fail<int>("no")));

    public static readonly Parser<char, int> Reference = Rec(() => Value);

    public static readonly Parser<char, int> StaticReference = Rec(static () => Value);

    public static readonly Parser<char, int> Qualified = Rec(() => Names.Value);

    public static readonly Parser<char, (int, int)> Nested = Seq(Return(1), Rec(() => Return(2)));
}

public class RecTests
{
    private static RecNode<TIn, TOut> AsRec<TIn, TOut>(Parser<TIn, TOut> parser) => Assert.IsType<RecNode<TIn, TOut>>(parser);

    private static HashSet<INode> Reachable(INode root)
    {
        var seen = new HashSet<INode>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<INode>([root]);

        while (pending.TryPop(out var node))
        {
            if (seen.Add(node))
            {
                foreach (var child in node.Children)
                {
                    pending.Push(child);
                }
            }
        }

        return seen;
    }

    [Fact]
    public void ADefinitionSiteRecIsNamedAfterItsField() =>
        Assert.Equal("Value", AsRec(Names.Value).Name);

    [Fact]
    public void ANestedRecIsNamedAfterTheEnclosingField()
    {
        var rec = Assert.Single(Reachable(Names.Nested).OfType<RecNode<char, int>>());

        Assert.Equal("Nested", rec.Name);
    }

    [Fact]
    public void AReferenceIsNamedAfterTheMemberItReturns()
    {
        Assert.Equal("Value", AsRec(Names.Reference).Name);
        Assert.Equal("Value", AsRec(Names.StaticReference).Name);
        Assert.Equal("Names.Value", AsRec(Names.Qualified).Name);
    }

    [Fact]
    public void AForwardFieldReferenceResolvesToTheLaterField()
    {
        var rec = AsRec(Forward.First);

        Assert.Same(Forward.Second, rec.Target);
        Assert.Same(Forward.Second, Assert.Single(((INode)rec).Children));
    }

    [Fact]
    public void ANullReferenceIsAGrammarException()
    {
        Func<Parser<char, int>?> reference = null!;

        var exception = Assert.Throws<GrammarException>(() => Rec(reference));

        Assert.Equal("'reference' is null.", exception.Message);
    }

    [Fact]
    public void ANullTargetIsAGrammarExceptionAndIsNotKept()
    {
        Parser<char, int>? target = null;
        var rec = AsRec(Rec(() => target!));

        var exception = Assert.Throws<GrammarException>(() => rec.Target);
        target = Return(1);

        Assert.Equal("'() => target!' returned null. A Rec must refer to a rule that is initialized by the time the grammar is used.", exception.Message);
        Assert.Same(target, rec.Target);
    }

    [Fact]
    public void ExampleAIsAGraphWithCyclesThroughValue()
    {
        var value = AsRec(JsonGrammar.Value);
        var alternatives = Assert.IsType<OrNode<JTok, JsonValue>>(value.Target).Alternatives;

        Assert.Equal("Value", value.Name);
        Assert.Same(JsonGrammar.ArrayValue, alternatives[5]);
        Assert.Same(JsonGrammar.ObjectValue, alternatives[6]);
        Assert.Contains(JsonGrammar.Value, Reachable(JsonGrammar.ArrayValue));
        Assert.Contains(JsonGrammar.Value, Reachable(JsonGrammar.ObjectValue));
        Assert.Contains(JsonGrammar.Value, Reachable(JsonGrammar.Document));
    }

    [Fact]
    public void ExampleAResolvesToOneNodeEachTime()
    {
        var value = AsRec(JsonGrammar.Value);

        Assert.Same(value.Target, value.Target);
        Assert.Same(value.Target, Assert.Single(((INode)value).Children));
    }

    [Fact]
    public void TheDocsExampleIsACycleThroughItem()
    {
        var item = Assert.Single(Reachable(Lists.List).OfType<RecNode<char, int>>());

        Assert.Equal("List", item.Name);
        Assert.Same(Lists.List, item.Target);
    }

    [Fact]
    public void APlanWithACycleParsesRecursiveInput()
    {
        Assert.Equal(new Outcome.Ok("3", 12), Execution.Outcome(Lists.List, "[[],[[]],[]]"));
        Assert.Equal(new Outcome.FailConsumed(), Execution.Outcome(Lists.List, "[[],[]"));
    }

    [Fact]
    public void APlanWithACycleBetweenTwoRulesParsesRecursiveInput()
    {
        Assert.Equal(new Outcome.Ok("3", 7), Execution.Outcome(Parens.Group, "(((x)))"));
        Assert.Equal(new Outcome.Ok("0", 1), Execution.Outcome(Parens.Inner, "x"));
        Assert.Equal(new Outcome.FailConsumed(), Execution.Outcome(Parens.Group, "((x)"));
    }

    [Fact]
    public void APlanWithACycleParsesDeeplyNestedInput() =>
        Assert.Equal(new Outcome.Ok("500", 1001), Execution.Outcome(Parens.Group, new string('(', 500) + "x" + new string(')', 500)));

    [Fact]
    public void ExampleAParsesNestedTokens()
    {
        JTok[] tokens =
        [
            new(J.LBrace, "{"), new(J.String, "a"), new(J.Colon, ":"),
            new(J.LBracket, "["), new(J.Number, "1"), new(J.Comma, ","), new(J.LBracket, "["), new(J.RBracket, "]"), new(J.RBracket, "]"),
            new(J.RBrace, "}"),
        ];

        Assert.Equal(new Outcome.Ok("a:2", 10), Execution.Outcome(JsonGrammar.Document, tokens, static v =>
            v is JsonObject { Members: [var member] } && member.Value is JsonArray array ? $"{member.Key}:{array.Items.Count}" : "?"));
    }

    [Fact]
    public void RecIsANodeOfItsOwnKind() =>
        Assert.Equal(NodeKind.Rec, ((INode)Names.Reference).Kind);

    [Theory]
    [InlineData("() => Value", "Field", "Value")]
    [InlineData("static () => Value", "Field", "Value")]
    [InlineData("()=>Value", "Field", "Value")]
    [InlineData("() => JsonGrammar.Value", "Field", "JsonGrammar.Value")]
    [InlineData("() => @class", "Field", "@class")]
    [InlineData("() => OneOf(A, B)", "Value", "Value")]
    [InlineData("() => Value!", "Value", "Value")]
    [InlineData("reference", "Value", "Value")]
    [InlineData("() => OneOf(A, B)", ".cctor", null)]
    [InlineData("() => OneOf(A, B)", "<Main>$", null)]
    [InlineData("() => OneOf(A, B)", "", null)]
    [InlineData("() => .Value", "", null)]
    [InlineData("staticValue", "", null)]
    public void RuleNameComesFromTheReferenceThenTheCaller(string referenceExpression, string memberName, string? expected) =>
        Assert.Equal(expected, RuleName.From(referenceExpression, memberName));

    [Fact]
    public void TheReferenceIsCalledOnFirstUseNotWhenBuilt()
    {
        var calls = 0;
        var rec = AsRec(Rec(() =>
        {
            calls++;
            return Return(1);
        }));

        Assert.Equal(0, calls);

        var first = rec.Target;
        var second = rec.Target;

        Assert.Equal(1, calls);
        Assert.Same(first, second);
    }
}

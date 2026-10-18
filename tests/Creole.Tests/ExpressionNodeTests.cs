using Creole.Nodes;

namespace Creole.Tests;

public class ExpressionNodeTests
{
    private sealed class Calc : Grammar<char>
    {
        public static readonly Parser<char, double> Atom = Return(1.0);
        public static readonly Parser<char, char> Bang = Return('!');
        public static readonly Parser<char, char> Caret = Return('^');
        public static readonly Parser<char, char> Minus = Return('-');
        public static readonly Parser<char, char> Star = Return('*');
        public static readonly Parser<char, char> Slash = Return('/');
        public static readonly Parser<char, char> Plus = Return('+');
        public static readonly Parser<char, char> Less = Return('<');

        public static readonly Parser<char, double> Expr = Expression.Build(Atom, op =>
        [
            [op.Postfix(Bang, (x, _) => x)],
            [op.InfixR(Caret, (l, _, r) => Math.Pow(l, r))],
            [op.Prefix(Minus, (_, x) => -x)],
            [op.InfixL(Star, (l, _, r) => l * r), op.InfixL(Slash, (l, _, r) => l / r)],
            [op.InfixL(Plus, (l, _, r) => l + r), op.InfixL(Minus, (l, _, r) => l - r)],
            [op.InfixN(Less, (l, _, r) => l < r ? 1 : 0)],
        ]);
    }

    private static readonly Parser<char, int> _atom = Grammar<char>.Return(0);
    private static readonly Parser<char, char> _op = Grammar<char>.Return('+');

    [Fact]
    public void AnArithmeticTableBuildsOneExpressionNode()
    {
        var node = Assert.IsType<ExpressionNode<char, double>>(Calc.Expr);

        Assert.Equal(NodeKind.Expression, ((INode)node).Kind);
        Assert.Same(Calc.Atom, node.Atom);
    }

    [Fact]
    public void TheTableViewListsEveryLevelFixityAndOperatorInOrder()
    {
        var view = (IExpressionNode)Calc.Expr;

        Assert.Same(Calc.Atom, view.Atom);
        Assert.Equal(
            [
                [new(Fixity.Postfix, Calc.Bang)],
                [new(Fixity.InfixR, Calc.Caret)],
                [new(Fixity.Prefix, Calc.Minus)],
                [new(Fixity.InfixL, Calc.Star), new(Fixity.InfixL, Calc.Slash)],
                [new(Fixity.InfixL, Calc.Plus), new(Fixity.InfixL, Calc.Minus)],
                [new OperatorInfo(Fixity.InfixN, Calc.Less)],
            ],
            view.Levels.Select(level => level.ToArray())
        );
    }

    [Fact]
    public void ChildrenAreTheAtomThenEveryOperatorParserInTableOrder() =>
        Assert.Equal(
            [Calc.Atom, Calc.Bang, Calc.Caret, Calc.Minus, Calc.Star, Calc.Slash, Calc.Plus, Calc.Minus, Calc.Less],
            ((INode)Calc.Expr).Children
        );

    [Fact]
    public void TheOperatorsKeepTheirParsersAndCombineFunctions()
    {
        Func<int, char, int, int> add = (l, _, r) => l + r;
        Func<char, int, int> negate = (_, x) => -x;
        Func<int, char, int> twice = (x, _) => x * 2;

        var node = Assert.IsType<ExpressionNode<char, int>>(Grammar<char>.Expression.Build(_atom, op =>
        [
            [op.Prefix(_op, negate), op.Postfix(_op, twice), op.InfixR(_op, add)],
        ]));

        var prefix = Assert.IsType<PrefixOperator<char, char, int>>(node.Levels[0][0]);
        var postfix = Assert.IsType<PostfixOperator<char, char, int>>(node.Levels[0][1]);
        var infix = Assert.IsType<InfixOperator<char, char, int>>(node.Levels[0][2]);

        Assert.Same(_op, prefix.Op);
        Assert.Same(negate, prefix.Combine);
        Assert.Same(_op, postfix.Op);
        Assert.Same(twice, postfix.Combine);
        Assert.Same(_op, infix.Op);
        Assert.Same(add, infix.Combine);
        Assert.Equal(Fixity.InfixR, infix.Fixity);
    }

    [Fact]
    public void TheNodeKeepsACopyOfTheTable()
    {
        var level = new List<ExpressionOperator<char, int>>();
        var table = new List<IReadOnlyList<ExpressionOperator<char, int>>> { level };

        var node = (IExpressionNode)Grammar<char>.Expression.Build(_atom, op =>
        {
            level.Add(op.InfixL(_op, (l, _, r) => l + r));
            return table;
        });

        level.Add(level[0]);
        table.Add(level);

        var item = Assert.Single(view(node));
        Assert.Single(item);

        static IReadOnlyList<IReadOnlyList<OperatorInfo>> view(IExpressionNode node) => node.Levels;
    }

    [Fact]
    public void TheTableLambdaRunsOnce()
    {
        var runs = 0;

        Grammar<char>.Expression.Build(_atom, op =>
        {
            runs++;
            return [[op.InfixL(_op, (l, _, r) => l + r)]];
        });

        Assert.Equal(1, runs);
    }

    [Fact]
    public void PrefixAndPostfixMayShareALevelWithOneInfixAssociativity()
    {
        var node = (IExpressionNode)Grammar<char>.Expression.Build(_atom, op =>
        [
            [op.Prefix(_op, (_, x) => x), op.InfixN(_op, (l, _, r) => l), op.Postfix(_op, (x, _) => x), op.InfixN(_op, (l, _, r) => r)],
        ]);

        Assert.Equal([Fixity.Prefix, Fixity.InfixN, Fixity.Postfix, Fixity.InfixN], node.Levels[0].Select(op => op.Fixity));
    }

    public static TheoryData<string, string> InvalidTables => new()
    {
        { "empty table", "The operator table has no levels." },
        { "empty level", "Level 1 of the operator table has no operators." },
        { "null table", "The operator table is null." },
        { "null level", "Level 1 of the operator table is null." },
        { "null operator", "Operator 1 in level 0 of the operator table is null." },
        { "InfixL with InfixR", "Level 0 of the operator table mixes InfixL and InfixR operators. Put infix operators with different associativity on different levels." },
        { "InfixN with InfixL", "Level 1 of the operator table mixes InfixN and InfixL operators. Put infix operators with different associativity on different levels." },
        { "InfixR with InfixN", "Level 0 of the operator table mixes InfixR and InfixN operators. Put infix operators with different associativity on different levels." },
    };

    [Theory]
    [MemberData(nameof(InvalidTables))]
    public void AnInvalidTableThrowsGrammarException(string table, string message)
    {
        var exception = Assert.Throws<GrammarException>(() => Grammar<char>.Expression.Build(_atom, op => table switch
        {
            "empty table" => [],
            "empty level" => [[op.InfixL(_op, Add)], []],
            "null table" => null!,
            "null level" => [[op.InfixL(_op, Add)], null!],
            "null operator" => [[op.InfixL(_op, Add), null!]],
            "InfixL with InfixR" => [[op.InfixL(_op, Add), op.Prefix(_op, (_, x) => x), op.InfixR(_op, Add)]],
            "InfixN with InfixL" => [[op.InfixL(_op, Add)], [op.InfixN(_op, Add), op.InfixL(_op, Add)]],
            _ => [[op.InfixR(_op, Add), op.InfixN(_op, Add), op.InfixR(_op, Add)]],
        }));

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void BuildRejectsANullAtomNamingTheExpression()
    {
        Parser<char, int> operand = null!;

        var exception = Assert.Throws<GrammarException>(() => Grammar<char>.Expression.Build(operand, op => [[op.InfixL(_op, Add)]]));

        Assert.Equal(
            "'operand' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => operand).",
            exception.Message
        );
    }

    [Fact]
    public void BuildRejectsANullTableLambdaNamingTheExpression()
    {
        Func<OperatorFactory<char, int>, IReadOnlyList<IReadOnlyList<ExpressionOperator<char, int>>>> operators = null!;

        var exception = Assert.Throws<GrammarException>(() => Grammar<char>.Expression.Build(_atom, operators));

        Assert.Equal("'operators' is null.", exception.Message);
    }

    public static TheoryData<string> FactoryMethods => [nameof(OperatorFactory<,>.Prefix), nameof(OperatorFactory<,>.Postfix), nameof(OperatorFactory<,>.InfixL), nameof(OperatorFactory<,>.InfixR), nameof(OperatorFactory<,>.InfixN)];

    [Theory]
    [MemberData(nameof(FactoryMethods))]
    public void FactoryMethodsRejectANullOperatorParserNamingTheExpression(string method)
    {
        Parser<char, char> plus = null!;

        var exception = Assert.Throws<GrammarException>(() => Grammar<char>.Expression.Build(_atom, op =>
        [
            [method switch
            {
                nameof(op.Prefix) => op.Prefix(plus, (_, x) => x),
                nameof(op.Postfix) => op.Postfix(plus, (x, _) => x),
                nameof(op.InfixL) => op.InfixL(plus, Add),
                nameof(op.InfixR) => op.InfixR(plus, Add),
                _ => op.InfixN(plus, Add),
            }],
        ]));

        Assert.Equal(
            "'plus' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => plus).",
            exception.Message
        );
    }

    [Theory]
    [MemberData(nameof(FactoryMethods))]
    public void FactoryMethodsRejectANullCombineFunctionNamingTheExpression(string method)
    {
        Func<char, int, int> unary = null!;
        Func<int, char, int> post = null!;
        Func<int, char, int, int> binary = null!;

        var exception = Assert.Throws<GrammarException>(() => Grammar<char>.Expression.Build(_atom, op =>
        [
            [method switch
            {
                nameof(op.Prefix) => op.Prefix(_op, unary),
                nameof(op.Postfix) => op.Postfix(_op, post),
                nameof(op.InfixL) => op.InfixL(_op, binary),
                nameof(op.InfixR) => op.InfixR(_op, binary),
                _ => op.InfixN(_op, binary),
            }],
        ]));

        var expected = method switch
        {
            nameof(OperatorFactory<,>.Prefix) => "unary",
            nameof(OperatorFactory<,>.Postfix) => "post",
            _ => "binary",
        };

        Assert.Equal($"'{expected}' is null.", exception.Message);
    }

    private static int Add(int left, char op, int right) => left + right;
}

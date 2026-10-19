namespace Creole.Tests;

/// <summary>
/// A reference model of Creole's commit and cut semantics over <see cref="ControlShape"/> grammars.
/// </summary>
/// <remarks>
/// <para>
/// The rules it implements:
/// a cut passed inside an <c>Atomic</c> stops it rewinding;
/// a cut does not survive leaving a <c>Lookahead</c>, a <c>Not</c> or a <c>Longest</c> alternative;
/// and a cut is dropped with every attempt that fails without consuming.
/// An attempt is anything a light mark holds: each alternative of <c>Or</c>, the item of <c>Optional</c>, each repetition item whether mandatory or not, each <c>SepBy</c> separator, and each <c>Until</c> terminator.
/// The model covers <c>Many</c>, <c>AtLeastOnce</c>, <c>SepBy</c> and <c>Until</c>; <c>SepBy1</c>, <c>SepEndBy</c>, ranged <c>Repeat</c> and <c>Expression.Build</c> follow the same rule but are not modelled.
/// </para>
/// <para>
/// <see cref="Run"/> returns the outcome and whether a cut is in force afterwards.
/// Two engines that agree on both agree in every context, and a difference in either shows in some context:
/// the cut shows when the grammar is wrapped in <c>Atomic(Seq(prefix, grammar, rest))</c>, with a prefix that consumes and, if the grammar succeeds, a rest that then fails.
/// </para>
/// <para>
/// With <c>dispatch</c> set, the model skips every attempt that the next item shows would fail without consuming, as a generator's <c>switch</c> does.
/// The semantics make that exact, which <see cref="ControlModelTests"/> checks.
/// </para>
/// </remarks>
internal sealed class ControlModel(bool dispatch)
{
    public static ControlModel Dispatching { get; } = new(dispatch: true);

    public static ControlModel InOrder { get; } = new(dispatch: false);

    public bool Dispatch { get; } = dispatch;

    /// <summary>
    /// Every input over <c>a</c> and <c>b</c> up to <paramref name="length"/> characters.
    /// </summary>
    public static IEnumerable<string> AllInputs(int length) =>
        Enumerable.Range(0, length + 1).SelectMany(n => Enumerable.Range(0, 1 << n).Select(bits =>
            new string([.. Enumerable.Range(0, n).Select(i => (bits >> i & 1) == 0 ? 'a' : 'b')])));

    /// <summary>
    /// Every shape up to <paramref name="depth"/> over the leaves <c>a</c>, <c>b</c>, <c>ab</c> and <c>Cut</c>, with every unary control and repetition shape and binary <c>Seq</c>, <c>Or</c>, <c>Longest</c>, <c>SepBy</c> and <c>Until</c>.
    /// Repetitions that can loop without consuming are left out: a <c>Many</c>, <c>AtLeastOnce</c> or <c>Until</c> item that can succeed without consuming, or a <c>SepBy</c> whose item and separator both can.
    /// Analysis reports them (CRL0001), and engines guard against them at run time.
    /// </summary>
    public static IEnumerable<ControlShape> AllShapes(int depth)
    {
        ControlShape[] leaves = [new ControlShape.Chars("a"), new ControlShape.Chars("b"), new ControlShape.Chars("ab"), new ControlShape.Cut()];

        if (depth <= 1)
        {
            return leaves;
        }

        var smaller = AllShapes(depth - 1).ToArray();

        var unary = smaller.SelectMany(s => new ControlShape[]
        {
            new ControlShape.Atomic(s),
            new ControlShape.Lookahead(s),
            new ControlShape.Not(s),
            new ControlShape.Optional(s),
            new ControlShape.Many(s),
            new ControlShape.AtLeastOnce(s),
        });

        var binary = smaller.SelectMany(first => smaller.SelectMany(second => new ControlShape[]
        {
            new ControlShape.Seq(first, second),
            new ControlShape.Or(first, second),
            new ControlShape.Longest(first, second),
            new ControlShape.SepBy(first, second),
            new ControlShape.Until(first, second),
        }));

        return leaves.Concat(unary).Concat(binary).Where(s => !RepeatsNullable(s));
    }

    /// <summary>
    /// A compact description of a shape, such as <c>Atomic(Seq(a, Or(b, Seq(Cut, a))))</c>.
    /// </summary>
    public static string Describe(ControlShape shape) => shape switch
    {
        ControlShape.Chars s => s.Text,
        ControlShape.Cut => "Cut",
        ControlShape.Seq s => $"Seq({string.Join(", ", s.Steps.Select(Describe))})",
        ControlShape.Or s => $"Or({Describe(s.First)}, {Describe(s.Second)})",
        ControlShape.Longest s => $"Longest({Describe(s.First)}, {Describe(s.Second)})",
        ControlShape.SepBy s => $"SepBy({Describe(s.Item)}, {Describe(s.Separator)})",
        ControlShape.Until s => $"Until({Describe(s.Item)}, {Describe(s.Terminator)})",
        ControlShape.Atomic s => $"Atomic({Describe(s.Item)})",
        ControlShape.Lookahead s => $"Lookahead({Describe(s.Item)})",
        ControlShape.Not s => $"Not({Describe(s.Item)})",
        ControlShape.Optional s => $"Optional({Describe(s.Item)})",
        ControlShape.Many s => $"Many({Describe(s.Item)})",
        ControlShape.AtLeastOnce s => $"AtLeastOnce({Describe(s.Item)})",
        ControlShape.Named s => $"Named({Describe(s.Item)})",
        ControlShape.Context s => $"Context({Describe(s.Item)})",
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    /// <summary>
    /// Merges every <c>Seq</c> nested directly in a <c>Seq</c>, so <c>Seq(a, Seq(b, c))</c> becomes <c>Seq(a, b, c)</c>.
    /// </summary>
    public static ControlShape Flatten(ControlShape shape) => shape switch
    {
        ControlShape.Seq s => new ControlShape.Seq([.. s.Steps.Select(Flatten).SelectMany(step => step is ControlShape.Seq inner ? inner.Steps : [step])]),
        ControlShape.Or s => new ControlShape.Or(Flatten(s.First), Flatten(s.Second)),
        ControlShape.Longest s => new ControlShape.Longest(Flatten(s.First), Flatten(s.Second)),
        ControlShape.SepBy s => new ControlShape.SepBy(Flatten(s.Item), Flatten(s.Separator)),
        ControlShape.Until s => new ControlShape.Until(Flatten(s.Item), Flatten(s.Terminator)),
        ControlShape.Atomic s => new ControlShape.Atomic(Flatten(s.Item)),
        ControlShape.Lookahead s => new ControlShape.Lookahead(Flatten(s.Item)),
        ControlShape.Not s => new ControlShape.Not(Flatten(s.Item)),
        ControlShape.Optional s => new ControlShape.Optional(Flatten(s.Item)),
        ControlShape.Many s => new ControlShape.Many(Flatten(s.Item)),
        ControlShape.AtLeastOnce s => new ControlShape.AtLeastOnce(Flatten(s.Item)),
        ControlShape.Named s => new ControlShape.Named(Flatten(s.Item), s.Label),
        ControlShape.Context s => new ControlShape.Context(Flatten(s.Item), s.Text),
        _ => shape,
    };

    /// <summary>
    /// Runs <paramref name="shape"/> on <paramref name="input"/> from the start, with no cut in force.
    /// A repetition whose item succeeds without consuming gives <see cref="Outcome.Throws"/>.
    /// </summary>
    public (Outcome Outcome, bool CutInForce) Run(ControlShape shape, string input)
    {
        var evaluator = new Evaluator(Dispatch, input);

        try
        {
            var ok = evaluator.Eval(shape, out var value);
            Outcome outcome = ok ? new Outcome.Ok(value, evaluator.Position)
                : evaluator.Position > 0 ? new Outcome.FailConsumed()
                : new Outcome.FailEmpty();

            return (outcome, evaluator.Cut);
        }
        catch (NullableRepetitionException)
        {
            return (new Outcome.Throws(), false);
        }
    }

    /// <summary>
    /// How many attempts containing a <c>Cut</c> dispatch skips when running <paramref name="shape"/> on <paramref name="input"/>.
    /// Shows that the dispatch tests skip attempts that matter.
    /// </summary>
    public int SkippedAttemptsWithCuts(ControlShape shape, string input)
    {
        var evaluator = new Evaluator(Dispatch, input);

        try
        {
            evaluator.Eval(shape, out _);
        }
        catch (NullableRepetitionException)
        {
        }

        return evaluator.SkippedAttemptsWithCuts;
    }

    /// <summary>
    /// Whether <paramref name="shape"/> contains a <c>Cut</c>.
    /// </summary>
    internal static bool ContainsCut(ControlShape shape) => shape switch
    {
        ControlShape.Cut => true,
        ControlShape.Chars => false,
        ControlShape.Seq s => s.Steps.Any(ContainsCut),
        ControlShape.Or s => ContainsCut(s.First) || ContainsCut(s.Second),
        ControlShape.Longest s => ContainsCut(s.First) || ContainsCut(s.Second),
        ControlShape.SepBy s => ContainsCut(s.Item) || ContainsCut(s.Separator),
        ControlShape.Until s => ContainsCut(s.Item) || ContainsCut(s.Terminator),
        ControlShape.Atomic s => ContainsCut(s.Item),
        ControlShape.Lookahead s => ContainsCut(s.Item),
        ControlShape.Not s => ContainsCut(s.Item),
        ControlShape.Optional s => ContainsCut(s.Item),
        ControlShape.Many s => ContainsCut(s.Item),
        ControlShape.AtLeastOnce s => ContainsCut(s.Item),
        ControlShape.Named s => ContainsCut(s.Item),
        ControlShape.Context s => ContainsCut(s.Item),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    /// <summary>
    /// Whether the first item consumed by any outcome of <paramref name="shape"/> that keeps its consumption can be <paramref name="c"/>.
    /// </summary>
    internal static bool First(ControlShape shape, char c) => shape switch
    {
        ControlShape.Chars s => s.Text[0] == c,
        ControlShape.Cut or ControlShape.Not => false,
        ControlShape.Seq s => FirstOfSteps(s.Steps, c),
        ControlShape.Or s => First(s.First, c) || First(s.Second, c),
        ControlShape.Longest s => First(s.First, c) || First(s.Second, c),
        ControlShape.SepBy s => First(s.Item, c) || (Nullable(s.Item) && First(s.Separator, c)),
        ControlShape.Until s => First(s.Terminator, c) || First(s.Item, c),
        ControlShape.Atomic s => First(s.Item, c),
        ControlShape.Lookahead s => First(s.Item, c),
        ControlShape.Optional s => First(s.Item, c),
        ControlShape.Many s => First(s.Item, c),
        ControlShape.AtLeastOnce s => First(s.Item, c),
        ControlShape.Named s => First(s.Item, c),
        ControlShape.Context s => First(s.Item, c),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    /// <summary>
    /// Whether <paramref name="shape"/> may succeed without consuming input. Over-approximates.
    /// </summary>
    internal static bool Nullable(ControlShape shape) => shape switch
    {
        ControlShape.Chars => false,
        ControlShape.Cut or ControlShape.Not or ControlShape.Lookahead or ControlShape.Optional or ControlShape.Many or ControlShape.SepBy => true,
        ControlShape.Seq s => s.Steps.All(Nullable),
        ControlShape.Or s => Nullable(s.First) || Nullable(s.Second),
        ControlShape.Longest s => Nullable(s.First) || Nullable(s.Second),
        ControlShape.Until s => Nullable(s.Terminator),
        ControlShape.Atomic s => Nullable(s.Item),
        ControlShape.AtLeastOnce s => Nullable(s.Item),
        ControlShape.Named s => Nullable(s.Item),
        ControlShape.Context s => Nullable(s.Item),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    private static bool FirstOfSteps(IEnumerable<ControlShape> steps, char c)
    {
        foreach (var step in steps)
        {
            if (First(step, c))
            {
                return true;
            }

            if (!Nullable(step))
            {
                return false;
            }
        }

        return false;
    }

    private static bool RepeatsNullable(ControlShape shape) => shape switch
    {
        ControlShape.Many s => Nullable(s.Item) || RepeatsNullable(s.Item),
        ControlShape.AtLeastOnce s => Nullable(s.Item) || RepeatsNullable(s.Item),
        ControlShape.SepBy s => (Nullable(s.Item) && Nullable(s.Separator)) || RepeatsNullable(s.Item) || RepeatsNullable(s.Separator),
        ControlShape.Until s => Nullable(s.Item) || RepeatsNullable(s.Item) || RepeatsNullable(s.Terminator),
        ControlShape.Seq s => s.Steps.Any(RepeatsNullable),
        ControlShape.Or s => RepeatsNullable(s.First) || RepeatsNullable(s.Second),
        ControlShape.Longest s => RepeatsNullable(s.First) || RepeatsNullable(s.Second),
        ControlShape.Atomic s => RepeatsNullable(s.Item),
        ControlShape.Lookahead s => RepeatsNullable(s.Item),
        ControlShape.Not s => RepeatsNullable(s.Item),
        ControlShape.Optional s => RepeatsNullable(s.Item),
        ControlShape.Named s => RepeatsNullable(s.Item),
        ControlShape.Context s => RepeatsNullable(s.Item),
        _ => false,
    };
}

file sealed class NullableRepetitionException : Exception;

/// <summary>
/// One run of <see cref="ControlModel"/>: the input, the position and the cut flag.
/// </summary>
file sealed class Evaluator(bool dispatch, string input)
{
    public bool Cut { get; private set; }

    public int Position { get; private set; }

    public int SkippedAttemptsWithCuts { get; private set; }

    public bool Eval(ControlShape shape, out string value)
    {
        switch (shape)
        {
            case ControlShape.Chars s:
                value = s.Text;
                return EvalChars(s.Text);

            case ControlShape.Cut:
                Cut = true;
                value = "";
                return true;

            case ControlShape.Seq s:
                return EvalSeq(s.Steps, out value);

            case ControlShape.Or s:
                return EvalOr([s.First, s.Second], out value);

            case ControlShape.Longest s:
                return EvalLongest([s.First, s.Second], out value);

            case ControlShape.SepBy s:
                return EvalSepBy(s.Item, s.Separator, out value);

            case ControlShape.Until s:
                return EvalUntil(s.Item, s.Terminator, out value);

            case ControlShape.Atomic s:
                return EvalAtomic(s.Item, out value);

            case ControlShape.Lookahead s:
                return EvalLookahead(s.Item, out value);

            case ControlShape.Not s:
                value = "";
                return EvalNot(s.Item);

            case ControlShape.Optional s:
                return Attempt(s.Item, out value) is not AttemptResult.CommittedFailure;

            case ControlShape.Many s:
                return EvalRepeat(s.Item, 0, out value);

            case ControlShape.AtLeastOnce s:
                return EvalRepeat(s.Item, 1, out value);

            case ControlShape.Named s:
                return Eval(s.Item, out value);

            case ControlShape.Context s:
                return Eval(s.Item, out value);

            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    /// <summary>
    /// Runs <paramref name="attempt"/> as one attempt held by a light mark.
    /// </summary>
    private AttemptResult Attempt(ControlShape attempt, out string value) =>
        Attempt(attempt, (out string v) => Eval(attempt, out v), out value);

    /// <summary>
    /// Runs <paramref name="run"/> as one attempt held by a light mark: skipped if dispatch shows that <paramref name="shape"/> would fail without consuming, and its cut dropped if it fails without consuming.
    /// On an empty failure, <paramref name="value"/> is the empty string.
    /// </summary>
    private AttemptResult Attempt(ControlShape shape, Step run, out string value)
    {
        value = "";

        if (dispatch && !Viable(shape))
        {
            SkippedAttemptsWithCuts += ControlModel.ContainsCut(shape) ? 1 : 0;
            return AttemptResult.EmptyFailure;
        }

        var savedCut = Cut;
        var start = Position;

        if (run(out value))
        {
            return AttemptResult.Success;
        }

        if (Position != start)
        {
            return AttemptResult.CommittedFailure;
        }

        Cut = savedCut;
        value = "";
        return AttemptResult.EmptyFailure;
    }

    private bool EvalAtomic(ControlShape item, out string value)
    {
        var savedCut = Cut;
        var start = Position;
        Cut = false;

        var ok = Eval(item, out value);

        if (!ok && !Cut)
        {
            Position = start;
        }

        Cut = savedCut || Cut;
        return ok;
    }

    private bool EvalChars(string text)
    {
        foreach (var c in text)
        {
            if (Position >= input.Length || input[Position] != c)
            {
                return false;
            }

            Position++;
        }

        return true;
    }

    private bool EvalLongest(ControlShape[] alternatives, out string value)
    {
        var savedCut = Cut;
        var start = Position;
        var bestEnd = -1;
        value = "";

        foreach (var alternative in alternatives)
        {
            Position = start;
            Cut = false;

            if (Eval(alternative, out var v) && Position > bestEnd)
            {
                bestEnd = Position;
                value = v;
            }
        }

        Cut = savedCut;
        Position = bestEnd < 0 ? start : bestEnd;
        return bestEnd >= 0;
    }

    private bool EvalLookahead(ControlShape item, out string value)
    {
        var savedCut = Cut;
        var start = Position;
        Cut = false;

        var ok = Eval(item, out value);

        if (ok)
        {
            Position = start;
        }

        Cut = savedCut;
        return ok;
    }

    private bool EvalNot(ControlShape item)
    {
        var savedCut = Cut;
        var start = Position;
        Cut = false;

        var ok = Eval(item, out _);

        Position = start;
        Cut = savedCut;
        return !ok;
    }

    private bool EvalOr(ControlShape[] alternatives, out string value)
    {
        foreach (var alternative in alternatives)
        {
            switch (Attempt(alternative, out value))
            {
                case AttemptResult.Success:
                    return true;

                case AttemptResult.CommittedFailure:
                    return false;
            }
        }

        value = "";
        return false;
    }

    private bool EvalRepeat(ControlShape item, int min, out string value)
    {
        value = "";

        for (var count = 0; ; count++)
        {
            var start = Position;

            switch (Attempt(item, out var v))
            {
                case AttemptResult.Success when Position == start:
                    throw new NullableRepetitionException();

                case AttemptResult.Success:
                    value += v;
                    continue;

                case AttemptResult.CommittedFailure:
                    return false;

                default:
                    return count >= min;
            }
        }
    }

    private bool EvalSepBy(ControlShape item, ControlShape separator, out string value)
    {
        value = "";

        switch (Attempt(item, out var first))
        {
            case AttemptResult.CommittedFailure:
                return false;

            case AttemptResult.EmptyFailure:
                return true;
        }

        value = first;

        while (true)
        {
            var start = Position;

            // The separator and the item after it are separate attempts; the separator's value is discarded.
            switch (Attempt(separator, out _))
            {
                case AttemptResult.CommittedFailure:
                    return false;

                case AttemptResult.EmptyFailure:
                    return true;
            }

            switch (Attempt(item, out var next))
            {
                case AttemptResult.Success when Position == start:
                    throw new NullableRepetitionException();

                case AttemptResult.Success:
                    value += next;
                    continue;

                case AttemptResult.CommittedFailure:
                    return false;

                default:
                    // An item that fails without consuming ends the list only if the separator consumed nothing either.
                    return Position == start;
            }
        }
    }

    private bool EvalSeq(IEnumerable<ControlShape> steps, out string value)
    {
        value = "";

        foreach (var step in steps)
        {
            if (!Eval(step, out var v))
            {
                return false;
            }

            value += v;
        }

        return true;
    }

    private bool EvalUntil(ControlShape item, ControlShape terminator, out string value)
    {
        value = "";

        while (true)
        {
            switch (Attempt(terminator, out _))
            {
                case AttemptResult.Success:
                    return true;

                case AttemptResult.CommittedFailure:
                    return false;
            }

            var start = Position;

            switch (Attempt(item, out var v))
            {
                case AttemptResult.Success when Position == start:
                    throw new NullableRepetitionException();

                case AttemptResult.Success:
                    value += v;
                    continue;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Whether <paramref name="shape"/> could do anything but fail without consuming at the current position.
    /// </summary>
    private bool Viable(ControlShape shape) =>
        ControlModel.Nullable(shape) || (Position < input.Length && ControlModel.First(shape, input[Position]));
}

file delegate bool Step(out string value);

file enum AttemptResult
{
    Success,
    EmptyFailure,
    CommittedFailure,
}

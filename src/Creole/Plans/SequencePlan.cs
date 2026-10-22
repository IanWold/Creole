namespace Creole;

/// <summary>
/// Runs 2 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Func<T1, T2, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, TOut> _selector = selector;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2))
        {
            return false;
        }

        result = _selector(v1, v2);
        return true;
    }
}

/// <summary>
/// Runs 3 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, T3, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Plan<TIn, T3> third, Func<T1, T2, T3, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, T3, TOut> _selector = selector;
    private readonly Plan<TIn, T3> _third = third;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;
        T3 v3 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2) ||
            !_third.TryParse(ref cursor, ref v3))
        {
            return false;
        }

        result = _selector(v1, v2, v3);
        return true;
    }
}

/// <summary>
/// Runs 4 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, T3, T4, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Plan<TIn, T3> third, Plan<TIn, T4> fourth, Func<T1, T2, T3, T4, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T4> _fourth = fourth;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, T3, T4, TOut> _selector = selector;
    private readonly Plan<TIn, T3> _third = third;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;
        T3 v3 = default!;
        T4 v4 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2) ||
            !_third.TryParse(ref cursor, ref v3) ||
            !_fourth.TryParse(ref cursor, ref v4))
        {
            return false;
        }

        result = _selector(v1, v2, v3, v4);
        return true;
    }
}

/// <summary>
/// Runs 5 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, T3, T4, T5, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Plan<TIn, T3> third, Plan<TIn, T4> fourth, Plan<TIn, T5> fifth, Func<T1, T2, T3, T4, T5, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T5> _fifth = fifth;
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T4> _fourth = fourth;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, T3, T4, T5, TOut> _selector = selector;
    private readonly Plan<TIn, T3> _third = third;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;
        T3 v3 = default!;
        T4 v4 = default!;
        T5 v5 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2) ||
            !_third.TryParse(ref cursor, ref v3) ||
            !_fourth.TryParse(ref cursor, ref v4) ||
            !_fifth.TryParse(ref cursor, ref v5))
        {
            return false;
        }

        result = _selector(v1, v2, v3, v4, v5);
        return true;
    }
}

/// <summary>
/// Runs 6 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, T3, T4, T5, T6, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Plan<TIn, T3> third, Plan<TIn, T4> fourth, Plan<TIn, T5> fifth, Plan<TIn, T6> sixth, Func<T1, T2, T3, T4, T5, T6, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T5> _fifth = fifth;
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T4> _fourth = fourth;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, T3, T4, T5, T6, TOut> _selector = selector;
    private readonly Plan<TIn, T6> _sixth = sixth;
    private readonly Plan<TIn, T3> _third = third;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;
        T3 v3 = default!;
        T4 v4 = default!;
        T5 v5 = default!;
        T6 v6 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2) ||
            !_third.TryParse(ref cursor, ref v3) ||
            !_fourth.TryParse(ref cursor, ref v4) ||
            !_fifth.TryParse(ref cursor, ref v5) ||
            !_sixth.TryParse(ref cursor, ref v6))
        {
            return false;
        }

        result = _selector(v1, v2, v3, v4, v5, v6);
        return true;
    }
}

/// <summary>
/// Runs 7 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, T3, T4, T5, T6, T7, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Plan<TIn, T3> third, Plan<TIn, T4> fourth, Plan<TIn, T5> fifth, Plan<TIn, T6> sixth, Plan<TIn, T7> seventh, Func<T1, T2, T3, T4, T5, T6, T7, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T5> _fifth = fifth;
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T4> _fourth = fourth;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, T3, T4, T5, T6, T7, TOut> _selector = selector;
    private readonly Plan<TIn, T7> _seventh = seventh;
    private readonly Plan<TIn, T6> _sixth = sixth;
    private readonly Plan<TIn, T3> _third = third;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;
        T3 v3 = default!;
        T4 v4 = default!;
        T5 v5 = default!;
        T6 v6 = default!;
        T7 v7 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2) ||
            !_third.TryParse(ref cursor, ref v3) ||
            !_fourth.TryParse(ref cursor, ref v4) ||
            !_fifth.TryParse(ref cursor, ref v5) ||
            !_sixth.TryParse(ref cursor, ref v6) ||
            !_seventh.TryParse(ref cursor, ref v7))
        {
            return false;
        }

        result = _selector(v1, v2, v3, v4, v5, v6, v7);
        return true;
    }
}

/// <summary>
/// Runs 8 plans in order and combines their results. Fails as soon as one fails; the failure is committed if anything before it consumed input.
/// </summary>
internal sealed class SequencePlan<TIn, T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Plan<TIn, T1> first, Plan<TIn, T2> second, Plan<TIn, T3> third, Plan<TIn, T4> fourth, Plan<TIn, T5> fifth, Plan<TIn, T6> sixth, Plan<TIn, T7> seventh, Plan<TIn, T8> eighth, Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> selector) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, T8> _eighth = eighth;
    private readonly Plan<TIn, T5> _fifth = fifth;
    private readonly Plan<TIn, T1> _first = first;
    private readonly Plan<TIn, T4> _fourth = fourth;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> _selector = selector;
    private readonly Plan<TIn, T7> _seventh = seventh;
    private readonly Plan<TIn, T6> _sixth = sixth;
    private readonly Plan<TIn, T3> _third = third;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        T1 v1 = default!;
        T2 v2 = default!;
        T3 v3 = default!;
        T4 v4 = default!;
        T5 v5 = default!;
        T6 v6 = default!;
        T7 v7 = default!;
        T8 v8 = default!;

        if (!_first.TryParse(ref cursor, ref v1) ||
            !_second.TryParse(ref cursor, ref v2) ||
            !_third.TryParse(ref cursor, ref v3) ||
            !_fourth.TryParse(ref cursor, ref v4) ||
            !_fifth.TryParse(ref cursor, ref v5) ||
            !_sixth.TryParse(ref cursor, ref v6) ||
            !_seventh.TryParse(ref cursor, ref v7) ||
            !_eighth.TryParse(ref cursor, ref v8))
        {
            return false;
        }

        result = _selector(v1, v2, v3, v4, v5, v6, v7, v8);
        return true;
    }
}

namespace Creole;

/// <summary>
/// The run-time guard against a repetition that would loop forever because a round consumed nothing.
/// </summary>
/// <remarks>
/// Only unbounded repetitions check: <c>Many</c>, <c>AtLeastOnce</c>, <c>SkipMany</c>, <c>SkipAtLeastOnce</c>, the separated lists, <c>Until</c>, the operators of <c>Expression.Build</c>, which repeat, and layers, which repeat their item parser to the end of their input.
/// A bounded <c>Repeat</c> ends after its maximum, so it runs an item that consumes nothing that many times.
/// Grammar analysis will report these grammars before they run; until then, this guard turns a hang into an exception.
/// </remarks>
internal static class NullableRepetition
{
    public static GrammarException Item() =>
        new("A repeated parser succeeded without consuming input, so the repetition would never end. Make the item consume input, or bound the repetition with Repeat(min, max).");

    public static GrammarException InfixOperator() =>
        new("An infix operator and the operand after it succeeded without consuming input, so the expression would never end. Make the operator or the atom consume input.");

    public static GrammarException ItemAndSeparator() =>
        new("A separator and the item after it succeeded without consuming input, so the list would never end. Make the item or the separator consume input.");

    public static GrammarException LayerItem() =>
        new("A layer's item parser succeeded without consuming input, so the layer would never reach the end of its input. Make the item consume input, and put input to skip between items in skip.");

    public static GrammarException LayerStep() =>
        new("A layer's step parser succeeded without consuming input, so the layer would never reach the end of its input. Make the step consume input, and emit outputs that stand for no input as synthetic outputs of a step that does, or from atEnd.");

    public static GrammarException Operator() =>
        new("A prefix or postfix operator succeeded without consuming input, so the expression would never end. Make the operator consume input.");
}

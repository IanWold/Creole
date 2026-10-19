using BenchmarkDotNet.Attributes;

namespace Creole.Benchmarks;

[BenchmarkCategory("Arithmetic")]
public class ArithmeticBenchmarks
{
    private readonly IReadOnlyList<string> _expressions = ArithmeticInputs.Expressions;

    [Benchmark(Baseline = true, OperationsPerInvoke = ArithmeticInputs.Count)]
    public double HandWritten()
    {
        var sum = 0.0;
        foreach (var expression in _expressions)
        {
            sum += HandWrittenArithmetic.Evaluate(expression);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = ArithmeticInputs.Count)]
    public double Parlot()
    {
        var sum = 0.0;
        foreach (var expression in _expressions)
        {
            sum += ParlotArithmetic.Evaluate(ParlotArithmetic.Interpreted, expression);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = ArithmeticInputs.Count)]
    public double ParlotCompiled()
    {
        var sum = 0.0;
        foreach (var expression in _expressions)
        {
            sum += ParlotArithmetic.Evaluate(ParlotArithmetic.Compiled, expression);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = ArithmeticInputs.Count)]
    public double Pidgin()
    {
        var sum = 0.0;
        foreach (var expression in _expressions)
        {
            sum += PidginArithmetic.Evaluate(PidginArithmetic.OperatorTable, expression);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = ArithmeticInputs.Count)]
    public double PidginFolds()
    {
        var sum = 0.0;
        foreach (var expression in _expressions)
        {
            sum += PidginArithmetic.Evaluate(PidginArithmetic.Folds, expression);
        }

        return sum;
    }
}

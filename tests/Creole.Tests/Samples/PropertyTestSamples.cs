using System.Text;
using CsCheck;

namespace Creole.Tests.Samples;

public class PropertyTestSamples
{
    [Fact]
    public void Utf8LengthIsSumOfRuneLengths() =>
        Gen.String.Sample(text => Encoding.UTF8.GetByteCount(text) == text.EnumerateRunes().Sum(rune => rune.Utf8SequenceLength));

    [Fact]
    public void StackBehavesLikeListModel() =>
        Gen.Const(() => (new Stack<int>(), new List<int>())).SampleModelBased(
            Gen.Int.Operation<Stack<int>, List<int>>(
                i => $"Push({i})",
                (stack, i) => stack.Push(i),
                (model, i) => model.Add(i)
            ),

            Gen.Operation<Stack<int>, List<int>>(
                "Pop",
                stack => stack.TryPop(out _),
                model =>
                {
                    if (model.Count > 0)
                    {
                        model.RemoveAt(model.Count - 1);
                    }
                }
            ),

            equal: (stack, model) => stack.SequenceEqual(Enumerable.Reverse(model))
        );
}

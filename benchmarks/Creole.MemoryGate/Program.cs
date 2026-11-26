using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Creole.MemoryGate;

const long WarmUpLength = 1_000_000;
const int RecordsPerSample = 1_000;

if (args is not [var name, var lengthText]
    || GateInput.All.FirstOrDefault(input => input.Name == name) is not { } input
    || !long.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out var length))
{
    Console.Error.WriteLine($"Usage: Creole.MemoryGate <{string.Join('|', GateInput.All.Select(static input => input.Name))}> <characters>");
    return 2;
}

if (Parse(input, WarmUpLength, int.MaxValue, static () => { }) is null)
{
    Console.Error.WriteLine($"The {name} warm-up input did not parse.");
    return 1;
}

var baseline = Retained();
var peak = long.MinValue;
var samples = 0;
var stopwatch = Stopwatch.StartNew();

if (Parse(input, length, RecordsPerSample, () => { peak = Math.Max(peak, Retained()); samples++; }) is not var (characters, records))
{
    Console.Error.WriteLine($"The {name} input did not parse.");
    return 1;
}

Console.WriteLine(JsonSerializer.Serialize(new GateRun(name, length, characters, records, samples, baseline, peak, stopwatch.Elapsed.TotalSeconds), JsonSerializerOptions.Web));
return 0;

[MethodImpl(MethodImplOptions.NoInlining)]
static (long Characters, long Records)? Parse(GateInput input, long length, int recordsPerSample, Action sample)
{
    using var reader = new GeneratedReader(input, length, recordsPerSample, sample);

    return input.Parse(reader)
        ? (reader.Characters, reader.Records)
        : null;
}

static long Retained()
{
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

    var heap = GC.GetGCMemoryInfo(GCKind.FullBlocking);

    return heap.HeapSizeBytes - heap.FragmentedBytes;
}

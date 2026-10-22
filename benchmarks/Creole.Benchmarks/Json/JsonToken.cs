namespace Creole.Benchmarks;

public readonly record struct JsonToken(JsonKind Kind) : IKinded<JsonKind>;

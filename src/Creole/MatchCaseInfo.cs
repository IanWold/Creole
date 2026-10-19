namespace Creole;

/// <summary>
/// One case in the view: the union case type it applies to and its branch parser.
/// </summary>
internal readonly record struct MatchCaseInfo(Type CaseType, INode Branch);

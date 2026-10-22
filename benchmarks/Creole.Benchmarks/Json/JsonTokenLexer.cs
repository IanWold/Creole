namespace Creole.Benchmarks;

public static class JsonTokenLexer
{
    public static JsonToken[] Lex(string text)
    {
        var tokens = new List<JsonToken>();

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            JsonKind kind;

            switch (c)
            {
                case '{':
                    kind = JsonKind.LBrace;
                    break;
                case '}':
                    kind = JsonKind.RBrace;
                    break;
                case '[':
                    kind = JsonKind.LBracket;
                    break;
                case ']':
                    kind = JsonKind.RBracket;
                    break;
                case ':':
                    kind = JsonKind.Colon;
                    break;
                case ',':
                    kind = JsonKind.Comma;
                    break;
                case '"':
                    for (i++; text[i] != '"'; i += text[i] == '\\' ? 2 : 1)
                    {
                    }

                    kind = JsonKind.StringLiteral;
                    break;
                case 't':
                    i += "true".Length - 1;
                    kind = JsonKind.True;
                    break;
                case 'f':
                    i += "false".Length - 1;
                    kind = JsonKind.False;
                    break;
                case 'n':
                    i += "null".Length - 1;
                    kind = JsonKind.Null;
                    break;
                default:
                    while (i + 1 < text.Length && (char.IsAsciiDigit(text[i + 1]) || text[i + 1] is '.' or 'e' or 'E' or '+' or '-'))
                    {
                        i++;
                    }

                    kind = JsonKind.Number;
                    break;
            }

            tokens.Add(new JsonToken(kind));
        }

        return [.. tokens];
    }
}

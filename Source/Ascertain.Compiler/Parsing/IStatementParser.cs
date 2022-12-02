using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public interface IStatementParser
{
    BaseSyntacticExpression? ParseToken(Token token);
}
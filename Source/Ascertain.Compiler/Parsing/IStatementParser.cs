using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public interface IStatementParser
{
    BaseExpression? ParseToken(Token token);
}
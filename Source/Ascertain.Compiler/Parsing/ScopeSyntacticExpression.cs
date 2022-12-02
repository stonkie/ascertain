using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public record ScopeSyntacticExpression(Position Position, IReadOnlyCollection<BaseSyntacticExpression> Statements) : BaseSyntacticExpression(Position);
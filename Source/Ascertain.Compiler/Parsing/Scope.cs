using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public record Scope(Position Position, IReadOnlyCollection<BaseExpression> Statements) : BaseExpression(Position);
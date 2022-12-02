using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public record SyntacticObjectType(Position Position, string Name, Modifier Modifiers, List<SyntacticMember> Members,
    IReadOnlyList<CallSyntacticExpression> CompilerMetadata);

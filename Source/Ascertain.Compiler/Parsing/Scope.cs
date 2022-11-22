namespace Ascertain.Compiler.Parsing;

public class Scope : IScope
{
    public IReadOnlyCollection<IExpression> Statements { get; }

    public Scope(IReadOnlyCollection<IExpression> statements)
    {
        Statements = statements;
    }
}
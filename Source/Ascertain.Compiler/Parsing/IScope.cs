namespace Ascertain.Compiler.Parsing;

public interface IScope : IExpression
{
    IReadOnlyCollection<IExpression> Statements { get; }
}
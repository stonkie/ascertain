namespace Ascertain.Compiler.Parser;

public interface IScope : IExpression
{
    IReadOnlyCollection<IExpression> Statements { get; }
}
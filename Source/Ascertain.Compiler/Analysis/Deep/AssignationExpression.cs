namespace Ascertain.Compiler.Analysis.Deep;

public record AssignationExpression(Variable Variable, BaseExpression Source) : BaseExpression(Variable.ObjectType);
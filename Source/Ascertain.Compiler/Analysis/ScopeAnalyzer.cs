using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

public class ScopeAnalyzer
{
    private readonly TypeValidator _typeValidator;
    private readonly ScopeSyntacticExpression _scope;
    private readonly Dictionary<string, Variable> _variables;
    private readonly ITypeReference<ObjectType>? _acceptedReturnType;

    public ScopeAnalyzer(TypeValidator typeValidator, ScopeSyntacticExpression scope,
        IReadOnlyDictionary<string, Variable> variables, ITypeReference<ObjectType>? acceptedReturnType)
    {
        _typeValidator = typeValidator;
        _scope = scope;
        _variables = new Dictionary<string, Variable>(variables);
        _acceptedReturnType = acceptedReturnType;
    }
    
    public Scope Analyze()
    {
        List<BaseExpression> expressions = new();
        
        foreach (var statement in _scope.Statements)
        {
            expressions.Add(AnalyzeExpression(statement));
        }

        var scopeReturnType = expressions.LastOrDefault()?.ReturnType;

        if (scopeReturnType is not ObjectTypeReference objectReturnType)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerMethodReturnsAMethod,
                $"The method at {_scope.Position} cannot return a method itself.");
        }
        
        return new Scope(objectReturnType, expressions);
    }

    private BaseExpression AnalyzeExpression(BaseSyntacticExpression expression)
    {
        switch (expression)
        {
            case ScopeSyntacticExpression scope:
                var analyzer = new ScopeAnalyzer(_typeValidator, scope, _variables, _acceptedReturnType);
                return analyzer.Analyze();
            
            case AssignationSyntacticExpression assignation:
                return AnalyzeAssignation(assignation);
            
            case CallSyntacticExpression call:
                return AnalyzeCall(call);
                
            case AccessMemberSyntacticExpression accessMember:
                return AnalyzeAccessMember(accessMember);        
        
            default:
                throw new NotImplementedException();
        }
        
    }

    private BaseExpression AnalyzeAccessMember(AccessMemberSyntacticExpression accessMember)
    {
        var parentExpression = AnalyzeExpression(accessMember.Parent);
        
        // _typeValidator.AddImplicitCast(parentExpression.ReturnType.
        
        throw new NotImplementedException();
    }

    private BaseExpression AnalyzeCall(CallSyntacticExpression call)
    {
        // TODO : Handle call.IsStatic to decide if we're passing in "this"

        // TODO : Handle as method group to be resolved to specific method here during overload resolution
        BaseExpression method;
        
        if (call.Callable is AccessMemberSyntacticExpression callMember)
        {
            method = AnalyzeExpression(callMember);
        }
        else if (call.Callable is AccessVariableSyntacticExpression callVariable)
        {
            method = AnalyzeExpression(callVariable);
        }
        else
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerCallableIsNotAMember,
                $"The method call at {call.Position} must be made of an object member but is made on a different type of expression.");
        }

        if (method.ReturnType == null)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerCallableIsNothing,
                $"The method call at {call.Position} is made on a callable that is nothing.");
        }
        
        if (method.ReturnType is not AnonymousCallableType callableTypeReference)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerCallableIsNotAMethod,
                $"The method call at {call.Position} is made on a {method.ReturnType.ResolvedType} which is not a method.");
        }

        var callableType = callableTypeReference.ResolvedType; // Anonymous types are always resolved.
        
        List<BaseExpression> parameters = new();

        for (var index = 0; index < call.Parameters.Count; index++)
        {
            var callParameterSyntax = call.Parameters[index];

            var parameter = AnalyzeExpression(callParameterSyntax);

            if (parameter.ReturnType == null)
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerMethodParameterDoesNotReturnAValue,
                    $"The method call parameter at {callParameterSyntax.Position} does not return a value.");
            }
            
            if (parameter.ReturnType == null)
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerMethodParameterReturnsAMethod,
                    $"The method call parameter at {callParameterSyntax.Position} returns a method, should return a value.");
            }

            parameters.Add(parameter);
        }

        // TODO : Do overload resolution without resorting to AssignableTo
        bool isCallCompatibleWithSignature = callableType.AssignableTo(new AnonymousCallableType(call.Position, callableType.ReturnType,
            parameters.Select(p =>
            {
                if (p.ReturnType == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerParameterReturnsNothing,
                        $"A method call parameter at {call.Position} was accepted during analysis, but returns nothing.");
                }
                
                if (p.ReturnType is not ObjectTypeReference objectReturnType)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerParameterReturnsAMethod,
                        $"A method call parameter at {call.Position} was accepted during analysis, but returns a method.");
                }

                return new ParameterDeclaration(objectReturnType, "");
            }).ToList()));

        if (!isCallCompatibleWithSignature)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerMethodCallParametersDoNotMatchMethodDeclaration,
                $"The method call at {call.Position} is made using a parameter list ({string.Join(", ", parameters.Select(p => p.ReturnType))}) " +
                $"which does not match the method declaration ({string.Join(", ", callableType.Parameters)}).");
        }

        return new CallExpression(callableType.ReturnType, method, parameters);
    }

    private AssignationExpression AnalyzeAssignation(AssignationSyntacticExpression assignation)
    {
        Variable destination;
        if (assignation.Destination is AccessVariableSyntacticExpression accessDestinationVariable) // Turns read into write
        {
            if (!_variables.ContainsKey(accessDestinationVariable.Name))
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerAssignationDestinationVariableMustExist,
                    $"The destination variable {accessDestinationVariable.Name} of the assignation at {assignation.Position} does not exist.");
            }

            // TODO : Prevent writing to a non mutable variable 

            destination = _variables[accessDestinationVariable.Name];
        }
        // TODO : else if (assignation.Destination is DeclareVariableSyntacticExpression declareDestinationVariable)
        // TODO : Handle write to AccessMember (this is what needs to manage lifetimes (an function calls)
        else
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerAssignationDestinationMustBeAVariable,
                $"The destination of the assignation at {assignation.Position} is not a variable.");
        }

        var sourceExpression = AnalyzeExpression(assignation.Source);
        
        return new AssignationExpression(destination, sourceExpression);
    }
}

public class TypeValidator
{
    private readonly List<(ITypeReference<ObjectType> Destination, ITypeReference<ObjectType>? Source, Position Position)> _implicitCasts = new();

    public void AddImplicitCast(ObjectTypeReference destination, ObjectTypeReference? source, Position position)
    {
        if (destination.Name != source?.Name)
        {
            _implicitCasts.Add((destination, source, position));
        }
    }

    public void Validate()
    {
        foreach (var implicitCast in _implicitCasts)
        {
            var destinationType = implicitCast.Destination.ResolvedType;
            if (implicitCast.Source == null)
            {
                if (destinationType.Primitive?.Type != PrimitiveType.Void)
                {
                    throw new AscertainException(AscertainErrorCode.AnalyzerIncompatibleTypes,
                        $"At {implicitCast.Position}, no value is provided. A value that can be converted to type {implicitCast.Destination} is required.");
                }
            }
            else if (!implicitCast.Source.ResolvedType.AssignableTo(destinationType))
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerIncompatibleTypes,
                    $"At {implicitCast.Position}, variable of type {implicitCast.Source} cannot be implicitly converted to type {implicitCast.Destination}.");
            }
        }
    }
}

public abstract record BaseExpression(ITypeReference<BaseType> ReturnType);

public record CallExpression(ObjectTypeReference ObjectReturnType, BaseExpression Method, List<BaseExpression> Parameters) : BaseExpression(ObjectReturnType);

public record AssignationExpression(Variable Variable, BaseExpression Source) : BaseExpression(Variable.ObjectType);

public record Scope(ObjectTypeReference ObjectReturnType, IReadOnlyList<BaseExpression> Expressions) : BaseExpression(ObjectReturnType);
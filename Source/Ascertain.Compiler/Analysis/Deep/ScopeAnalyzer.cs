using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis.Deep;

public class ScopeAnalyzer
{
    private readonly IReadOnlyList<SurfaceObjectType> _accessibleSurfaceTypes;
    private readonly ScopeSyntacticExpression _scope;
    private readonly Dictionary<string, Variable> _variables;
    private readonly ITypeReference<SurfaceObjectType>? _acceptedReturnType;

    public ScopeAnalyzer(IReadOnlyList<SurfaceObjectType> accessibleSurfaceTypes, ScopeSyntacticExpression scope,
        IReadOnlyDictionary<string, Variable> variables, ITypeReference<SurfaceObjectType>? acceptedReturnType)
    {
        _accessibleSurfaceTypes = accessibleSurfaceTypes;
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
 
        var scopeReturnType = expressions.LastOrDefault()?.ReturnType ?? 
                _accessibleSurfaceTypes.Single(t => t.Primitive?.Type == PrimitiveType.Void);;

        if (scopeReturnType is not SurfaceObjectType objectReturnType)
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
                var analyzer = new ScopeAnalyzer(_accessibleSurfaceTypes, scope, _variables, _acceptedReturnType);
                return analyzer.Analyze();
            
            case AssignationSyntacticExpression assignation:
                return AnalyzeAssignation(assignation);
            
            case CallSyntacticExpression call:
                return AnalyzeCall(call);
                
            case AccessMemberSyntacticExpression accessMember:
                return AnalyzeAccessMember(accessMember);        
        
            case AccessVariableSyntacticExpression accessVariable:
                return AnalyzeAccessVariable(accessVariable);        

            default:
                throw new NotImplementedException();
        }
        
    }

    private BaseExpression AnalyzeAccessVariable(AccessVariableSyntacticExpression accessVariable)
    {
        if (accessVariable.Name.Length >= 2 && accessVariable.Name.StartsWith('"') && accessVariable.Name.EndsWith('"'))
        {
            string literalStringValue = accessVariable.Name[1..^1];

            try
            {
                return new ReadLiteralExpression(literalStringValue, _accessibleSurfaceTypes.Single(t => t.Name == QualifiedName.String));
            }
            catch (Exception ex)
            {
                throw new AscertainException(AscertainErrorCode.InternalErrorUnknownStringTypeClass,
                    $"The basic type String required for literal at position {accessVariable.Position} is undefined.");   
            }
        }

        if (accessVariable.IsCompilerDirective)
        {
            switch (accessVariable.Name)
            {
                case "stderr_print":
                    return new ReadBackboneFunctionExpression(new SurfaceCallableType(
                        new BoundObjectTypeReference(accessVariable.Position, _accessibleSurfaceTypes.Single(t => t.Primitive?.Type == PrimitiveType.Void)), 
                        new List<SurfaceParameterDeclaration>()
                        {
                            new(new BoundObjectTypeReference(accessVariable.Position, _accessibleSurfaceTypes.Single(t => t.Name == QualifiedName.String)), "content"),
                        }
                    ));
                
                default:
                    throw new AscertainException(AscertainErrorCode.AnalyzerUnknownDirective,
                        $"The compiler directive {accessVariable.Name} at position {accessVariable.Position} is unknown.");
            }
        }

        if (!_variables.ContainsKey(accessVariable.Name))
        {
            var possibleTypeAccess = _accessibleSurfaceTypes.SingleOrDefault(t => t.Name == new QualifiedName(accessVariable.Name));

            if (possibleTypeAccess == null)
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerUnresolvedVariable,
                    $"The variable {accessVariable.Name} referenced at {accessVariable.Position} does not exist.");    
            }
            
            return new ReadStaticTypeExpression(possibleTypeAccess); 
        }

        return new ReadVariableExpression(_variables[accessVariable.Name]);
    }

    private BaseExpression AnalyzeAccessMember(AccessMemberSyntacticExpression accessMember)
    {
        var parentExpression = AnalyzeExpression(accessMember.Parent);

        if (parentExpression.ReturnType is not SurfaceObjectType parentType)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerAttemptedAccessToMemberOfAMethod,
                $"There are no member to access on non object type {parentExpression.ReturnType} referenced at {accessMember.Position}.");
        }

        if (!parentType.Members.ContainsKey(accessMember.MemberName))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerAttemptedAccessToNonUndefinedMember,
                $"There are no member named {accessMember.MemberName} on object type {parentExpression.ReturnType} referenced at {accessMember.Position}.");
        }

        if (parentType.Members[accessMember.MemberName].Count != 1)
        {
            // TODO : Support overload resolution
            
            throw new AscertainException(AscertainErrorCode.AnalyzerAttemptedAccessToOverloadedMember,
                $"There are multiple members named {accessMember.MemberName} on object type {parentExpression.ReturnType} referenced at {accessMember.Position}.");
        }

        var returnType = parentType.Members[accessMember.MemberName].Single().ReturnType;

        return new ReadMemberExpression(returnType.ResolvedType, parentExpression, accessMember.MemberName, parentType);
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
        
        if (method.ReturnType is not SurfaceCallableType callableType)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerCallableIsNotAMethod,
                $"The method call at {call.Position} is made on a {method.ReturnType} which is not a method.");
        }

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
        bool isCallCompatibleWithSignature = callableType.AssignableTo(new AnonymousSurfaceCallableType(call.Position, callableType.ReturnType,
            parameters.Select(p =>
            {
                if (p.ReturnType == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerParameterReturnsNothing,
                        $"A method call parameter at {call.Position} was accepted during analysis, but returns nothing.");
                }
                
                if (p.ReturnType is not SurfaceObjectType objectReturnType)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerParameterReturnsAMethod,
                        $"A method call parameter at {call.Position} was accepted during analysis, but returns a method.");
                }

                return new SurfaceParameterDeclaration(new BoundObjectTypeReference(call.Position, objectReturnType), "");
            }).ToList()));

        if (!isCallCompatibleWithSignature)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerMethodCallParametersDoNotMatchMethodDeclaration,
                $"The method call at {call.Position} is made using a parameter list ({string.Join(", ", parameters.Select(p => p.ReturnType))}) " +
                $"which does not match the method declaration ({string.Join(", ", callableType.Parameters)}).");
        }

        return new CallExpression(callableType.ReturnType.ResolvedType, method, parameters);
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

public record ReadStaticTypeExpression(SurfaceObjectType ObjectType) : BaseExpression(ObjectType);

public record ReadMemberExpression(ISurfaceType ReturnType, BaseExpression ParentObject, string MemberName, SurfaceObjectType ParentReferenceType) : BaseExpression(ReturnType);

public record ReadVariableExpression(Variable Variable) : BaseExpression(Variable.ObjectType);

public record ReadLiteralExpression(string Value, ISurfaceType ReturnType) : BaseExpression(ReturnType);

public record ReadBackboneFunctionExpression(SurfaceCallableType CallableType) : BaseExpression(CallableType);


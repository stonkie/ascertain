using System.Drawing;
using System.Reflection.Metadata;
using System.Text;
using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Analysis.Deep;
using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Parsing;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Generation;

public class ProgramGenerator
{
    private LLVMModuleRef _module;
    private readonly Func<SurfaceObjectType, ObjectType> _analyzedTypeProvider;

    private readonly Lazy<LLVMTypeRef> _pointerType = new(() =>
    {
        var anyType = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
        return LLVMTypeRef.CreatePointer(anyType, 0);
    });
    
    private readonly Dictionary<QualifiedName, ObjectType> _remainingTypes = new();
    private readonly Dictionary<QualifiedName, TypeDeclaration> _declaredTypes = new();
    private readonly Dictionary<string, MethodGenerator> _methodGenerators = new(); 
    
    public ProgramGenerator(LLVMModuleRef module, SurfaceObjectType topLevelType, Func<SurfaceObjectType, ObjectType> analyzedTypeProvider)
    {
        _module = module;
        _analyzedTypeProvider = analyzedTypeProvider;
        _remainingTypes.Add(topLevelType.Name, _analyzedTypeProvider(topLevelType));
    }

    public (LLVMValueRef Function, LLVMTypeRef FunctionType) Write()
    {
        if (_pointerType.IsValueCreated)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorIsReused, "LLVM generator has already been used and cannot be reused.");
        }

        _ = _pointerType.Value;
        
        while (_remainingTypes.Any())
        {
            var nextPair = _remainingTypes.First();
            
            foreach (ObjectType dependency in WriteTypeDeclaration(nextPair.Value))
            {
                if (!_declaredTypes.ContainsKey(dependency.Name) && 
                    !_remainingTypes.ContainsKey(dependency.Name))
                {
                    _remainingTypes.Add(dependency.Name, dependency);
                }
            }
            
            _remainingTypes.Remove(nextPair.Key);
        }

        (LLVMValueRef Function, LLVMTypeRef FunctionType)? mainLlvmFunction = null;
        
        foreach (var methodGenerator in _methodGenerators)
        {
            var llvmFunction = methodGenerator.Value.Write();

            // TODO : Find main function in a cleaner way and throw right.
            if (methodGenerator.Key.Equals("Function_Program_New", StringComparison.InvariantCulture))
            {
                mainLlvmFunction = llvmFunction;
            }
        }
        
        if (!_module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string message))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"LLVM verification failed with message : {message}.");
        }

        // TODO : Find main function in a cleaner way and throw right.
        return mainLlvmFunction!.Value!;
    }

    private IEnumerable<ObjectType> WriteTypeDeclaration(ObjectType type)
    {
        Dictionary<string, LLVMValueRef> methodDeclarations = new();

        foreach (var member in type.Members)
        {
            if (member.Member.ReturnType is SurfaceCallableType method)
            {
                string llvmFunctionName = type.GetMangledName(member.Member, _analyzedTypeProvider);
                var returnType = _analyzedTypeProvider(method.ReturnType.ResolvedType);
                
                yield return returnType;

                List<LLVMTypeRef> passByParameterTypes = new();
                
                foreach (var parameter in method.Parameters)
                {
                    var parameterType = _analyzedTypeProvider(parameter.ObjectType.ResolvedType);
                    yield return parameterType;
                    passByParameterTypes.Add(GetPassByType(parameterType));
                }
                
                var functionType = LLVMTypeRef.CreateFunction(GetPassByType(returnType), passByParameterTypes.ToArray());
                var function = _module.AddFunction(llvmFunctionName, functionType);
                
                foreach (ObjectType implementationDependency in GetDependencies(member.Member.Expression))
                {
                    yield return implementationDependency;
                }
                
                methodDeclarations.Add(member.Name, function);

                _methodGenerators.Add(llvmFunctionName, new MethodGenerator(function, functionType, member.Member, method, _analyzedTypeProvider, _declaredTypes, _methodGenerators, _module, _pointerType.Value));
            }
            else
            {
                throw new NotImplementedException("Property is not implemented yet");
            }
        }

        if (_declaredTypes.ContainsKey(type.Name))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorTypeGeneratedMultipleTimes, $"Type was generated multiple times : {type.Name}.");
        }

        LLVMTypeRef typeRef = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
        _declaredTypes.Add(type.Name, new TypeDeclaration(typeRef, methodDeclarations));
    }

    private IEnumerable<ObjectType> GetDependencies(BaseExpression expression)
    {
        switch (expression.ReturnType)
        {
            case SurfaceObjectType objectType:
                yield return _analyzedTypeProvider(objectType);
                break;
            case SurfaceCallableType callableType:
                yield return _analyzedTypeProvider(callableType.ReturnType.ResolvedType);
                foreach (var parameter in callableType.Parameters)
                {
                    yield return _analyzedTypeProvider(parameter.ObjectType.ResolvedType);
                }
                break;
            default:
                throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorTypeClassHasNoDeclarationImplementation, $"Unknown type class during declaration generation : {expression.ReturnType}.");
        }

        IEnumerable<ObjectType> subDependencies;
        
        switch (expression)
        {
            case AssignationExpression assignationExpression:
                subDependencies = GetDependencies(assignationExpression.Source);
                break;
            case CallExpression callExpression:
                subDependencies = GetDependencies(callExpression.Method)
                    .Union(callExpression.Parameters.SelectMany(p => GetDependencies(p)));
                break;
            case ReadMemberExpression readMemberExpression:
                subDependencies = GetDependencies(readMemberExpression.ParentObject);
                break;
            case Scope scope:
                subDependencies = scope.Expressions.SelectMany(e => GetDependencies(e));
                break;
                
            case NewExpression:
            case ReadBackboneFunctionExpression:
            case ReadLiteralExpression:
            case ReadStaticTypeExpression readStaticTypeExpression:
            case ReadVariableExpression readVariableExpression:
                subDependencies = Array.Empty<ObjectType>();
                break;
            default:
                throw new NotImplementedException("Not implemented type dependency analyser");
        }

        foreach (var subDependency in subDependencies)
        {
            yield return subDependency;
        }
    }

    private LLVMTypeRef GetPassByType(ObjectType type)
    {
        if (type.Primitive != null)
        {
            switch (type.Primitive.Type)
            {
                case PrimitiveType.Void:
                    return _module.Context.VoidType;
                default:
                    throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorUnknownPrimitiveType, $"Unknown primitive type during generation : {type.Primitive.Type}.");        
            }
        }

        return _pointerType.Value;
    }
}
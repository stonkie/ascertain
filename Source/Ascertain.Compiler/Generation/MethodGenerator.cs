using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Analysis.Deep;
using Ascertain.Compiler.Analysis.Surface;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Generation;

public class MethodGenerator
{
    
    
    private readonly LLVMTypeRef _llvmFunctionType;
    private readonly LLVMValueRef _llvmFunction;
    private readonly Member _method;
    private readonly SurfaceCallableType _methodType;
    private readonly Func<SurfaceObjectType, ObjectType> _analyzedTypeProvider;
    private readonly Dictionary<QualifiedName, TypeDeclaration> _typeDeclarations;
    private readonly Dictionary<string, MethodGenerator> _methodGenerators;
    private LLVMModuleRef _module;
    private readonly LLVMTypeRef _llvmPointerType;

    public MethodGenerator(LLVMValueRef llvmFunction, 
        LLVMTypeRef llvmFunctionType, 
        Member method, 
        SurfaceCallableType methodType,
        Func<SurfaceObjectType, ObjectType> analyzedTypeProvider,
        Dictionary<QualifiedName, TypeDeclaration> typeDeclarations,
        Dictionary<string, MethodGenerator> methodGenerators,
        LLVMModuleRef module, 
        LLVMTypeRef llvmPointerType)
    {
        _llvmFunctionType = llvmFunctionType;
        _llvmFunction = llvmFunction;
        _method = method;
        _methodType = methodType;
        _analyzedTypeProvider = analyzedTypeProvider;
        _typeDeclarations = typeDeclarations;
        _methodGenerators = methodGenerators;
        _module = module;
        _llvmPointerType = llvmPointerType;
    }

    public (LLVMValueRef Function, LLVMTypeRef FunctionType) Write()
    {
        ObjectType returnType = _analyzedTypeProvider(_methodType.ReturnType.ResolvedType);
        
        Dictionary<string, LLVMValueRef> namedVariables = new();
        
        for (int parameterIndex = 0; parameterIndex < _llvmFunction.Params.Length; parameterIndex++)
        {
            string variableName = _methodType.Parameters[parameterIndex].Name;
            _llvmFunction.Params[parameterIndex].Name = variableName;
            
            namedVariables[variableName] = _llvmFunction.Params[parameterIndex];
        }
        
        var block = _llvmFunction.AppendBasicBlock("");
        using var builder = _module.Context.CreateBuilder();
        builder.PositionAtEnd(block);
        
        WriteExpression(builder, _method.Expression);
        
        if (returnType.Primitive != null)
        {
            switch (returnType.Primitive.Type)
            {
                case PrimitiveType.Void:
                    builder.BuildRetVoid();
                    break;
                default:
                    throw new NotImplementedException($"Primitive type is not implemented {returnType.Primitive.Type}");
            }
        }
        else
        {
            builder.BuildRet(LLVMValueRef.CreateConstNull(_llvmPointerType));    
        }
        
        if (!_llvmFunction.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"LLVM verification failed for function : {_method.Name}.");
        }

        return (_llvmFunction, _llvmFunctionType);
    }
    
    private (LLVMValueRef Value, LLVMTypeRef Type)? WriteExpression(LLVMBuilderRef builder, BaseExpression expression)
    {
        switch (expression)
        {
            case NewExpression newExpression:
                // var type = _analyzedTypeProvider(newExpression.SurfaceType);
                // var llvmType = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
                // // return LLVMTypeRef.CreatePointer(anyType, 0);
                //
                // var allocation = builder.BuildAlloca(llvmType);
                // allocation.
                break;
            case Scope scope:
                foreach (var subExpression in scope.Expressions)
                {
                    WriteExpression(builder, subExpression); 
                }
                break;
            case CallExpression callExpression:
            {
                var llvmCallTarget = WriteExpression(builder, callExpression.Method);

                if (llvmCallTarget == null)
                {
                    // TODO : Recover error when new is implemented
                    // throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorCallTargetIsNull, $"LLVM generation has no target for call.");
                    return null;
                }

                // TODO : Use return and pass as "this" parameter

                List<LLVMValueRef> llvmParameters = new();

                foreach (BaseExpression parameter in callExpression.Parameters)
                {
                    var llvmParameter = WriteExpression(builder, parameter);

                    if (llvmParameter == null)
                    {
                        throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorCallParameterIsNull,
                            $"LLVM generation has no parameter for call.");
                    }

                    llvmParameters.Add(llvmParameter.Value.Value);
                }


                var llvmReturnValue = builder.BuildCall2(llvmCallTarget.Value.Type, llvmCallTarget.Value.Value, llvmParameters.ToArray());

                // TODO : Returned value needs to be typed and converted... Wrapped?
                return (llvmReturnValue, _llvmPointerType);
            }
            case ReadMemberExpression readMemberExpression:
            {
                WriteExpression(builder, readMemberExpression.ParentObject);

                // TODO : Overload resolution, polymorphism, dynamic dispatch and/or momorphisation
                ObjectType parentType = _analyzedTypeProvider(readMemberExpression.ParentReferenceType);
                Member member = parentType.Members.Single(m => m.Name.Equals(readMemberExpression.MemberName)).Member;

                MethodGenerator targetGenerator = _methodGenerators[parentType.GetMangledName(member, _analyzedTypeProvider)];
                return (targetGenerator._llvmFunction, targetGenerator._llvmFunctionType);
            }
            case ReadLiteralExpression readLiteralException:
            {
                LLVMValueRef llvmGlobalString = builder.BuildGlobalString(readLiteralException.Value);

                return (llvmGlobalString, _llvmPointerType);
            }
            case ReadVariableExpression readVariableExpression:
            {
                // TODO : Implement read variable and new()

                return null;
            }
            case ReadBackboneFunctionExpression readBackboneFunctionExpression:
            {
                // TODO : Hardcode print

                // var functionType = LLVMTypeRef.CreateFunction(_module.Context.VoidType, Array.Empty<LLVMTypeRef>());
                // var externalFunction = _module.AddFunction("stderr_print", functionType);

                
                // return (externalFunction, functionType);
                return null;
            }
            default:
                // TODO : Not implemented
                break;
        }

        return null;
    }

}
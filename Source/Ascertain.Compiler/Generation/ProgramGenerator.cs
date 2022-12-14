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
    private readonly Dictionary<QualifiedName, LLVMTypeRef> _generatedTypes = new();
    
    public ProgramGenerator(LLVMModuleRef module, SurfaceObjectType topLevelType, Func<SurfaceObjectType, ObjectType> analyzedTypeProvider)
    {
        _module = module;
        _analyzedTypeProvider = analyzedTypeProvider;
        _remainingTypes.Add(topLevelType.Name, _analyzedTypeProvider(topLevelType));
    }

    public void Write()
    {
        if (_pointerType.IsValueCreated)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorIsReused, "LLVM generator has already been used and cannot be reused.");
        }

        _ = _pointerType.Value;
            
        while (_remainingTypes.Any())
        {
            var nextPair = _remainingTypes.First();
            
            foreach (ObjectType dependency in WriteType(nextPair.Value))
            {
                if (!_generatedTypes.ContainsKey(dependency.Name) && !_remainingTypes.ContainsKey(dependency.Name))
                {
                    _remainingTypes.Add(dependency.Name, dependency);
                }
            }
            
            _remainingTypes.Remove(nextPair.Key);
        }
        
        if (!_module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string message))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"LLVM verification failed with message : {message}.");
        }
    }

    private IEnumerable<ObjectType> WriteType(ObjectType type)
    {
        foreach (var member in type.Members.Values.SelectMany(m => m))
        {
            string memberName = GetMangledName(type, member);

            if (member.ReturnType is SurfaceCallableType method)
            {
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
                var function = _module.AddFunction(memberName, functionType);
                Dictionary<string, LLVMValueRef> namedVariables = new();

                for (int parameterIndex = 0; parameterIndex < function.Params.Length; parameterIndex++)
                {
                    string variableName = method.Parameters[parameterIndex].Name;
                    function.Params[parameterIndex].Name = variableName;
                    
                    namedVariables[variableName] = function.Params[parameterIndex];
                }

                var block = function.AppendBasicBlock("");
                var builder = _module.Context.CreateBuilder();
                builder.PositionAtEnd(block);

                if (memberName == "Function_Program_New_System")
                {
                    
                    // var externalFunctionType = LLVMTypeRef.CreateFunction(_module.Context.VoidType, new LLVMTypeRef[]{});
                    
                    // var externalFunction = _module.AddFunction("External_Test", externalFunctionType);
                    
                    
                    
                    // builder.BuildCall2(externalFunctionType, externalFunction, new LLVMValueRef[] { });
                }

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
                    builder.BuildRet(LLVMValueRef.CreateConstNull(_pointerType.Value));    
                }
                
                if (!function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction))
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"LLVM verification failed for function : {memberName}.");
                }
            }
            else
            {
                throw new NotImplementedException("Property is not implemented yet");
            }
        }

        if (_generatedTypes.ContainsKey(type.Name))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorTypeGeneratedMultipleTimes, $"Type was generated multiple times : {type.Name}.");
        }

        LLVMTypeRef typeRef = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
        _generatedTypes.Add(type.Name, typeRef);
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

    private string GetMangledName(ObjectType parentType, Member member)
    {
        if (member.ReturnType is SurfaceObjectType)
        {
            return $"Property_{parentType.Name}_{member.Name}";
        }

        if (member.ReturnType is SurfaceCallableType methodType)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Function_");
            builder.Append(parentType.Name);
            builder.Append("_");
        
            builder.Append(member.Name);
            builder.Append("_");
        
            foreach (var parameter in methodType.Parameters)
            {
                builder.Append(GetMangledName(_analyzedTypeProvider(parameter.ObjectType.ResolvedType)));
                builder.Append("_");
            }

            builder.Remove(builder.Length - 1, 1); // Remove trailing separator

            return builder.ToString();
        }

        throw new AscertainException(AscertainErrorCode.InternalErrorUnknownTypeClass, $"Unknown type class : {member.ReturnType}.");
    }

    private string GetMangledName(ObjectType parentType)
    {
        // TODO : Implement generics
        return parentType.Name.ToString();
    }
}
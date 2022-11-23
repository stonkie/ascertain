using System.Drawing;
using System.Text;
using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Parsing;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Generation;

public class ProgramGenerator
{
    private LLVMModuleRef _module;
    
    private readonly Lazy<LLVMTypeRef> _pointerType = new(() =>
    {
        var anyType = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
        return LLVMTypeRef.CreatePointer(anyType, 0);
    });
    
    
    
    private readonly Dictionary<QualifiedName, ObjectType> _remainingTypes = new();
    private readonly Dictionary<QualifiedName, LLVMTypeRef> _generatedTypes = new();
    
    public ProgramGenerator(LLVMModuleRef module, ObjectType topLevelType)
    {
        _module = module;
        _remainingTypes.Add(topLevelType.Name, topLevelType);
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
            string name = GetMangledName(type, member);

            if (member.Parameters != null)
            {
                var returnType = member.ReturnType.Get();
                
                yield return returnType;

                List<LLVMTypeRef> passByParameterTypes = new();
                
                foreach (var parameter in member.Parameters)
                {
                    var parameterType = parameter.ObjectType.Get();
                    yield return parameterType;
                    passByParameterTypes.Add(GetPassByType(parameterType));
                }
                
                var functionType = LLVMTypeRef.CreateFunction(GetPassByType(returnType), passByParameterTypes.ToArray());
                var function = _module.AddFunction(name, functionType);
                var block = function.AppendBasicBlock("");
                var builder = _module.Context.CreateBuilder();
                builder.PositionAtEnd(block);
                builder.BuildRet(LLVMValueRef.CreateConstNull(_pointerType.Value));

                if (!function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction))
                {
                    //function.
                    throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"LLVM verification failed for function : {name}.");
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

    private string GetMangledName(ObjectType type, Member member)
    {
        if (member.Parameters == null)
        {
            return $"Property_{type.Name}_{member.Name}";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("Function_");
        builder.Append(type.Name);
        builder.Append("_");
        
        builder.Append(member.Name);
        builder.Append("_");
        
        foreach (var parameter in member.Parameters)
        {
            builder.Append(GetMangledName(parameter.ObjectType));
            builder.Append("_");
        }

        builder.Remove(builder.Length - 1, 1); // Remove trailing separator

        return builder.ToString();
    }

    private string GetMangledName(ObjectTypeReference typeReference)
    {
        return typeReference.Get().Name.ToString();
    }
}
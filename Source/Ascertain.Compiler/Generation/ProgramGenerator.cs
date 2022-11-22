using System.Text;
using Ascertain.Compiler.Analysis;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Generation;

public class ProgramGenerator
{
    private LLVMModuleRef _module;
    
    private readonly Dictionary<QualifiedName, ObjectType> _remainingTypes = new();
    private readonly Dictionary<QualifiedName, LLVMTypeRef> _generatedTypes = new();

    public ProgramGenerator(LLVMModuleRef module, ObjectType topLevelType)
    {
        _module = module;
        _remainingTypes.Add(topLevelType.Name, topLevelType);
    }

    public void Write()
    {
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
                
                var functionType = LLVMTypeRef.CreateFunction(GetPassByType(returnType), new LLVMTypeRef[] { });
                var function = _module.AddFunction(name, functionType);
                var block = function.AppendBasicBlock("");
                var builder = _module.Context.CreateBuilder();
                builder.PositionAtEnd(block);
                builder.BuildRetVoid();

                if (!function.VerifyFunction(LLVMVerifierFailureAction.LLVMReturnStatusAction))
                {
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
            throw new AscertainException(AscertainErrorCode.InternalErrorTypeGeneratedMultipleTimes, $"Type was generated multiple times : {type.Name}.");
        }

        LLVMTypeRef typeRef = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
        _generatedTypes.Add(type.Name, typeRef);
    }

    private LLVMTypeRef GetPassByType(ObjectType type)
    {
        return LLVMTypeRef.Void;

        // TODO : Need to implement attributes, plus the "Primitive("void")" attribute 
        // if (type.Name == QualifiedName.Void) // TODO : Also needs to be non-extensible (when extensible types are added)
        // {
        //     return LLVMTypeRef.Void;
        // }
        // else if (type.Name == QualifiedName.Void)
        // {
        //     return LLVMTypeRef.Void;
        // }
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
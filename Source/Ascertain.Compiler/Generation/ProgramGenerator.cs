using System.Text;
using Ascertain.Compiler.Analysis;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Generation;

public class ProgramGenerator
{
    private LLVMModuleRef _module;
    private readonly ObjectType _programType;

    public ProgramGenerator(LLVMModuleRef module, ObjectType programType)
    {
        _module = module;
        _programType = programType;
    }

    public void Write()
    {
        foreach (var member in _programType.Members.Values.SelectMany(m => m))
        {
            string name = GetMangledName(_programType, member);
            
            var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, new LLVMTypeRef[] { });
            var function = _module.AddFunction(name, functionType);
            var block = function.AppendBasicBlock("");
            var builder = _module.Context.CreateBuilder();
            builder.PositionAtEnd(block);
            builder.BuildRetVoid();
        }

        if (!_module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string message))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"LLVM verification failed with message : {message}.");
        }
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
        if (typeReference.ResolvedType == null)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorUnresolvedReference, $"Unresolved reference to {typeReference.Name} reached the Generator.");
        }
            
        return typeReference.ResolvedType.Name.ToString();
    }
}
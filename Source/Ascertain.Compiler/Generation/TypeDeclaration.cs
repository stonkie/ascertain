using LLVMSharp.Interop;

namespace Ascertain.Compiler.Generation;

public record TypeDeclaration(LLVMTypeRef TypeReference, Dictionary<string, LLVMValueRef> MethodReferences);
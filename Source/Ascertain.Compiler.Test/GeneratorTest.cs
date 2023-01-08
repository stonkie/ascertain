using System.Diagnostics;
using System.Runtime.InteropServices;
using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Generation;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Test;

public class GeneratorTest
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr NewProgramDelegate(IntPtr program);

    [Fact]
    public void BasicGeneration()
    {
        var input = @"class Program { 
            public static New Program(System system) {
                system.GetFileSystem();

                new ();
            }
        }";

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);
        Parser parser = new(lexer.GetTokens());
        ProgramAnalyzer analyser = new(parser.GetTypes(), "Program");
        (var programType, var typeResolver) = analyser.GetProgramType().GetAwaiter().GetResult();
        
        using var module = LLVMModuleRef.CreateWithName("ascertain_program");
        ProgramGenerator generator = new(module, programType, typeResolver);

        generator.Write();

        // TODO : Move to generator, make it call the initialization and asc entry point 
        var anyType = LLVMTypeRef.CreateStruct(new LLVMTypeRef[] {}, false);
        var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, new[] {LLVMTypeRef.Int32, LLVMTypeRef.CreatePointer(anyType, 0)});
        var function = module.AddFunction("main", functionType);
        var block = function.AppendBasicBlock("");
        var builder = module.Context.CreateBuilder();
        builder.PositionAtEnd(block);

        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
                
        if (!function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorVerifierFailed, $"TEST.");
        }
        
        
        Assert.Equal(0, LLVM.InitializeNativeTarget());
        Assert.Equal(0, LLVM.InitializeNativeAsmParser());
        Assert.Equal(0, LLVM.InitializeNativeAsmPrinter());

        module.WriteBitcodeToFile("test.bc");
        var clang = Process.Start("clang", "test.bc -o test.exe");
        clang.WaitForExit();
        
        var engine = module.CreateMCJITCompiler();
        var entryPoint = engine.FindFunction("Function_Program_New_System");
        var entryPointDelegate = engine.GetPointerToGlobal<NewProgramDelegate>(entryPoint);
        
        try
        {
            entryPointDelegate(IntPtr.Zero);
        }
        catch (Exception ex)
        {
            throw;
        }
        
        
    }
    
    // TODO : These will need some code samples and tests for success and failures.
    
    // References tagged "own" are top-level (not needed on local variable declaration).
    // Assignation or return with the "lend" keyword produce child references.
    // "inject" are special cases of passing child references as parameter where the lifetime must be at least that of the callee to be saved to a member.

    // Keywords are "inject" and "own" on the function declaration.
    // Using the "lend" keyword creates a child reference. A variable holding a top-level or child reference are declared the same.
    // A top-level reference is any parameter received by "own", 
    // - When passing a reference as a parameter without the "lend" keyword, the object is given to the callee and cannot be reused.
    // - Only top-level references may be passed to "own" parameters and the callee becomes responsible for destroying the object after itself.
    // -- Child references cannot outlive the top-level reference.
    // - A "own" parameter is thereof a top-level reference by definition.
    // - A top-level reference is mutable if it has no child reference. A child reference is always immutable.
    // - For "inject" parameters, if a top-level reference is passed, it will destroy it as if it owned it. <= Should it be disallowed for performance concerns?
    // - For "inject" parameters, child references may be passed only if their lifetime is at least that of the callee.
    // Parameters that save to a member must be tagged "inject" or "own"
    // - A "own" or "inject" reference can be saved to an immutable member during construction.
    // - A "own" or "inject" reference can be saved to a mutable member any time..
    // - A "own" reference can be forwarded to another "own" parameter.
    // - A "own" cannot be forwarded to an "inject" parameter (but a child reference to an immutable member it is saved to can).
    // - An "inject" reference can be forwarded to another function's "inject" parameter if the callee's lifetime <= this' lifetime. 
    // - Only immutable members inherit the lifetime of the object they are members of

    

    // Possible syntax for internal mutability
    // class Test {
    //     public own Data Data;
    // 
    //     public static New Test()
    //     {
    //         return Test {
    //             Data = Data {
    //                 Field1 = 12,
    //                 Field2 = SomeObject.New() // consistent type must own all of its members
    //             };
    //         };
    //     }
    //
    //     public DoSomething own SomeObject()
    //     {
    //         // this.Data.Field1 = 3; // Illegal
    //         Data data = this.Data; // Only one child reference can exist. This takes a mutex until the child reference is destroyed.
    //         data.Field1 = 3;
    //         data.Field2.Mutate();
    //         // return lend data.Field2; // Illegal because it would create a child reference that outlives the mutex.
    //         return data.Field2.Copy();
    //     }
    // }
    // 
    // consistent Data {
    //     int Field1;
    //     SomeObject Field2;
    // }
}
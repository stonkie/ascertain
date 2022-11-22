using System.Runtime.InteropServices;
using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Generation;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;
using LLVMSharp.Interop;

namespace Ascertain.Compiler.Test;

public class GeneratorTest
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NewProgramDelegate();

    [Fact]
    public void BasicAnalysis()
    {
        var input = @"class Program { 
            public static New Program(System system) {
                system.GetFileSystem();
            }
        }";

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);
        Parser parser = new(lexer.GetTokens());
        ProgramAnalyzer analyser = new(parser.GetTypes(), "Program");
        var programType = analyser.GetProgramType().GetAwaiter().GetResult();
        
        using var module = LLVMModuleRef.CreateWithName("ascertain_program");
        ProgramGenerator generator = new(module, programType);

        generator.Write();
        
        Assert.Equal(0, LLVM.InitializeNativeTarget());
        Assert.Equal(0, LLVM.InitializeNativeAsmParser());
        Assert.Equal(0, LLVM.InitializeNativeAsmPrinter());
        
        var engine = module.CreateMCJITCompiler();
        var entryPoint = engine.FindFunction("Function_Program_New_System");
        var entryPointDelegate = engine.GetPointerToGlobal<NewProgramDelegate>(entryPoint);
        entryPointDelegate();
        
    }
}
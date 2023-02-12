using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Test;

public class AnalyzerTest
{
    [Fact]
    public void BasicAnalysis()
    {
        var input =
            """
            class Program { 
                public Initialize Void(System system) {
                    #stderr_print("Test Output");
                }
            }
            """;

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);
        Parser parser = new(lexer.GetTokens());
        ProgramAnalyzer analyser = new(parser.GetTypes(), "Program");

        var program = analyser.GetProgramType().GetAwaiter().GetResult();

        var constructor = program.SoughtType.Members["Initialize"].Single();

        Assert.True(constructor.IsPublic);
        Assert.Equal("Initialize", constructor.Name);
    }
    
    [Fact]
    public void TypeParameterAnalysis()
    {
        var input =
            """
            class Program { 
                public Initialize Void<ConsoleSystem Console>(System system) {
                    
                }
            }
            """;

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);
        Parser parser = new(lexer.GetTokens());
        ProgramAnalyzer analyser = new(parser.GetTypes(), "Program");

        var program = analyser.GetProgramType().GetAwaiter().GetResult();

        var constructor = program.SoughtType.Members["Initialize"].Single();

        Assert.True(constructor.IsPublic);
        var methodType = Assert.IsAssignableFrom<SurfaceCallableType>(constructor.ReturnType);
        
        var parameter = Assert.Single(methodType.Parameters);
        Assert.Equal("System", parameter.ObjectType.ResolvedType.Name.Name);
        Assert.Equal("system", parameter.Name);
        
        var typeParameter = Assert.Single(methodType.TypeParameters);
        Assert.Equal("ConsoleSystem", typeParameter.ObjectType.ResolvedType.Name.Name);
        Assert.Equal("Console", typeParameter.Name);
    }
}
using Ascertain.Compiler.Parser;

namespace Ascertain.Compiler.Test;

public class ParserTest
{
    [Fact]
    public void BasicParser()
    {
        var input = @"public class Program { 
            public static new void(System system) {
                system.GetFileSystem();
            }
        }";
        
        using StringReader reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens();
        var objects = new Parser.Parser(tokens).GetTypes().ToListAsync();
        var programObject = objects.GetAwaiter().GetResult().Single();

        Assert.Equal("Program", programObject.Name);
        Assert.Equal(Modifier.Class | Modifier.Public, programObject.Modifiers);
        
        Assert.Single(programObject.Members);

        var member = programObject.Members.Single();
        Assert.Equal("new", member.Name);
        Assert.Equal(Modifier.Public | Modifier.Static, member.Modifiers);
        
        var methodScope = Assert.IsType<Scope>(member.Statement);
        Assert.Equal(1, methodScope.Statements.Count); // TODO : Expand on this
        
        var methodType = Assert.IsType<Member>(member).TypeDeclaration;
        Assert.Equal("void", methodType.ReturnTypeName);
        // Assert.Equal(1, methodType.ParameterDeclarations.Count); // TODO : Expand on this
        
        
    }
}
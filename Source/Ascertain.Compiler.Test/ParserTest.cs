using System.Globalization;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Test;

public class ParserTest
{
    [Fact]
    public void BasicParser()
    {
        var input = @"class Program { 
            public static New Program(System system) {
                system.GetFileSystem();
            }
        }";

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens();
        var objects = new Parser(tokens).GetTypes().ToListAsync();
        var programObject = objects.GetAwaiter().GetResult().Single();

        Assert.Equal("Program", programObject.Name);
        Assert.Equal(Modifier.Class, programObject.Modifiers);

        Assert.Single(programObject.Members);

        var member = programObject.Members.Single();
        Assert.Equal("New", member.Name);
        Assert.Equal(Modifier.Public | Modifier.Static, member.Modifiers);

        // Method content
        {
            var methodScope = Assert.IsType<ScopeSyntacticExpression>(member.Expression);
            Assert.Equal(1, methodScope.Statements.Count);

            var callExpression = Assert.IsType<CallSyntacticExpression>(methodScope.Statements.Single());
            Assert.Empty(callExpression.Parameters);

            var accessMemberExpression = Assert.IsType<AccessMemberSyntacticExpression>(callExpression.Callable);
            Assert.Equal("GetFileSystem", accessMemberExpression.MemberName);

            var variableExpression = Assert.IsType<AccessVariableSyntacticExpression>(accessMemberExpression.Parent);
            Assert.Equal("system", variableExpression.Name);
        }

        var methodType = Assert.IsType<SyntacticMember>(member).TypeDeclaration;
        Assert.Equal("Program", methodType.ReturnTypeName);
        Assert.NotNull(methodType.ParameterDeclarations);
        Assert.Equal(1, methodType.ParameterDeclarations!.Count); // TODO : Expand on this
    }
    
    [Fact]
    public void ParseWithAttribute()
    {
        var input = 
        """
        #Primitive("void");
        class Void {}
        """;

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens();
        var objects = new Parser(tokens).GetTypes().ToListAsync();
        var voidType = objects.GetAwaiter().GetResult().Single();

        Assert.Equal("Void", voidType.Name);
        Assert.Equal(Modifier.Class, voidType.Modifiers);

        Assert.Empty(voidType.Members);
    }
}
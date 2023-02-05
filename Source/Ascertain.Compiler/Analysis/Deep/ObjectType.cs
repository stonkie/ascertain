using System.Text;
using Ascertain.Compiler.Analysis.Surface;

namespace Ascertain.Compiler.Analysis.Deep;

public record ObjectType(QualifiedName Name, List<(string Name, Member Member)> Members, CompilerPrimitiveType? Primitive)
{
    public string GetMangledName(Member member, Func<SurfaceObjectType, ObjectType> analyzedTypeProvider)
    {
        if (member.ReturnType is SurfaceObjectType)
        {
            return $"Property_{Name}_{member.Name}";
        }

        if (member.ReturnType is SurfaceCallableType methodType)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Function_");
            builder.Append(Name);
            builder.Append("_");
        
            builder.Append(member.Name);
            builder.Append("_");
        
            foreach (var parameter in methodType.Parameters)
            {
                builder.Append(GetMangledName(analyzedTypeProvider(parameter.ObjectType.ResolvedType)));
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
namespace Ascertain.Compiler.Analysis.Surface;

public interface ISurfaceType
{
    public bool AssignableTo(ISurfaceType destination);
}
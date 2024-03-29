﻿using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis;

public class SurfaceTypeRepository
{
    private readonly Dictionary<QualifiedName, SurfaceObjectType> _analyzedTypes = new();
    private readonly Dictionary<QualifiedName, List<ObjectTypeReference>> _references = new();

    public bool Contains(QualifiedName typeName)
    {
        return _analyzedTypes.ContainsKey(typeName);
    }

    public void Add(QualifiedName name, SurfaceObjectType baseType)
    {
        _analyzedTypes.Add(name, baseType);

        if (_references.ContainsKey(name))
        {
            foreach (var reference in _references[name])
            {
                reference.ResolvedType = baseType;
            }
        }
    }

    public ObjectTypeReference GetTypeReference(Position position, QualifiedName name)
    {
        // TODO : Make sure the name is fully qualified here (don't use plain strings?)
        ObjectTypeReference reference = new(position, name);

        if (!_references.ContainsKey(name))
        {
            _references.Add(name, new List<ObjectTypeReference>());
        }
        
        if (_analyzedTypes.ContainsKey(name))
        {
            reference.ResolvedType = _analyzedTypes[name];
        }
        
        _references[name].Add(reference);

        return reference;
    }

    public IReadOnlyCollection<ObjectTypeReference> GetUnresolvedTypeReferences()
    {
        return _references.Values.SelectMany(r => r).Where(r => r.ResolvedType == null).ToList();
    }

    public List<SurfaceObjectType> GetAllTypes()
    {
        return _analyzedTypes.Values.ToList();
    }
}
﻿using System.Collections.Concurrent;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

public class ProgramAnalyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types;
    private readonly string _programTypeName;
    private Task<ObjectType>? _programType;

    public ProgramAnalyzer(IAsyncEnumerable<SyntacticObjectType> types, string programTypeName)
    {
        _types = types;
        _programTypeName = programTypeName;
    }

    public async Task<ObjectType> GetProgramType()
    {
        _programType ??= AnalyzeProgramType();

        return await _programType;
    }

    private async Task<ObjectType> AnalyzeProgramType()
    {
        Analyzer analyzer = new(_types, _programTypeName);
        return await analyzer.GetType();
    }
}

internal class Analyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types; // TODO : Make types stream filterable by namespace
    private readonly string _soughtType; // Either a Program (new is main) or an Exposed (all public are C extern functions) object. 

    private readonly Dictionary<string, ObjectType> _analyzedObjects = new();

    public Analyzer(IAsyncEnumerable<SyntacticObjectType> types, string soughtType)
    {
        _types = types;
        _soughtType = soughtType;
    }

    public async Task<ObjectType> GetType()
    {
        ObjectType? soughtObjectType = null;
        
        // TODO : Prioritize search into referenced namespaces/files, trim unreferenced namespaces
        // TODO : Parallelize by file/parser and collect exceptions instead of aborting everything
        await foreach (SyntacticObjectType type in _types)
        {
            if (_analyzedObjects.ContainsKey(type.Name))
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerMultipleTypesWithTheSameName, $"A type of name {type.Name} was found {type.Position}, but had already been found before.");
            }
            else
            {
                ObjectType objectType = AnalyzeType(type);
                _analyzedObjects.Add(type.Name, objectType);

                if (type.Name == _soughtType)
                {
                    soughtObjectType = objectType;
                }
            }
        }

        if (soughtObjectType == null)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerSoughtTypeNotFound, $"The sought type {_soughtType} was not found.");
        }
        
        return soughtObjectType;
    }

    private ObjectType AnalyzeType(SyntacticObjectType type)
    {
        if (!type.Modifiers.HasFlag(Modifier.Class))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerNoCategoryModifierOnType, $"The type {type.Name} does not have the class modified.");
        }
        
        if (type.Modifiers.HasFlag(Modifier.Static))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerInvalidModifierOnType, $"The type {type.Name} has the illegal static modifier.");
        }

        if (type.Modifiers.HasFlag(Modifier.Public))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerInvalidModifierOnType, $"The type {type.Name} has the illegal public modifier.");
        }

        foreach (var member in type.Members)
        {
            AnalyzeMember(member);
        }
        
        return new ObjectType();
    }

    private void AnalyzeMember(Member member)
    {
        
    }
}

public class ObjectType
{
}
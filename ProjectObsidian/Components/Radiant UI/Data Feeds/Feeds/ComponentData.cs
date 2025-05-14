﻿using System;
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;

namespace Obsidian;

public class ComponentData
{
    private Type _componentType;

    public bool Submitted { get; private set; }

    public string MainName
    {
        get
        {
            return _componentType.Name;
        }
    }

    public string uniqueId;

    public Type ComponentType => _componentType;

    public bool IsGenericType => ComponentType.IsGenericType;

    public Type GenericTypeDefinition => IsGenericType ? ComponentType.GetGenericTypeDefinition() : null;

    public ComponentData(Type type)
    {
        this._componentType = type;
    }

    public void MarkSubmitted()
    {
        if (Submitted)
        {
            throw new InvalidOperationException("This item is already marked as submitted");
        }
        Submitted = true;
    }

    public void ClearSubmitted()
    {
        if (!Submitted)
        {
            throw new InvalidOperationException("This item isn't marked as submitted");
        }
        Submitted = false;
    }

    public bool MatchesSearchParameters(List<string> optionalTerms, List<string> requiredTerms, List<string> excludedTerms)
    {
        foreach (string excludedTerm in excludedTerms)
        {
            if (MatchesTerm(excludedTerm))
            {
                return false;
            }
        }
        foreach (string requiredTerm in requiredTerms)
        {
            if (!MatchesTerm(requiredTerm))
            {
                return false;
            }
        }
        if (requiredTerms.Count > 0)
        {
            return true;
        }
        if (optionalTerms.Count == 0)
        {
            return true;
        }
        foreach (string optionalTerm in optionalTerms)
        {
            if (MatchesTerm(optionalTerm))
            {
                return true;
            }
        }
        return false;
    }

    public bool MatchesTerm(string term)
    {
        if (ContainsTerm(MainName, term))
        {
            return true;
        }
        return false;
    }

    private static bool ContainsTerm(string str, string term)
    {
        if (string.IsNullOrEmpty(str))
        {
            return false;
        }
        return str.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public override string ToString()
    {
        return $"Name: {MainName}, UniqueID: {uniqueId}";
    }
}
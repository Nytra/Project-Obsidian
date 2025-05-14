using System.Collections.Generic;
using System;
using FrooxEngine;
using Microsoft.CodeAnalysis.Operations;

namespace Obsidian;

internal class ComponentsDataFeedData
{
    private List<TypeData> _data = new List<TypeData>();

    private Dictionary<string, TypeData> _dataByUniqueId = new Dictionary<string, TypeData>();

    public IEnumerable<TypeData> ComponentData => _data;

    public void Clear()
    {
        _data.Clear();
        _dataByUniqueId.Clear();
    }

	private string GetUniqueId(Type type)
	{
		return type.GetHashCode().ToString();
	}

	private TypeData RegisterComponentType(Type type, out bool createdEntry)
	{
		TypeData data = EnsureEntry(type, out createdEntry);

		if (!createdEntry)
		{
			throw new InvalidOperationException("Component with this Type has already been added! Type: " + type.FullName);
		}

		return data;
	}

	public TypeDataResult AddComponentType(Type type)
	{
		bool createdEntry;
		return new TypeDataResult(RegisterComponentType(type, out createdEntry), (!createdEntry) ? DataFeedItemChange.Updated : DataFeedItemChange.Added);
	}

	public TypeDataResult RemoveComponentType(Type type)
	{
		if (!_dataByUniqueId.TryGetValue(GetUniqueId(type), out var value))
		{
			return new TypeDataResult(null, DataFeedItemChange.Unchanged);
		}
		RemoveEntry(value);
		return new TypeDataResult(value, DataFeedItemChange.Removed);
	}

	private void RemoveEntry(TypeData data)
    {
        _data.Remove(data);
        _dataByUniqueId.Remove(data.uniqueId);
    }

	private TypeData EnsureEntry(Type type, out bool created)
	{
		if (_dataByUniqueId.TryGetValue(GetUniqueId(type), out var value))
		{
			created = false;
			return value;
		}
		value = new TypeData(type);
		value.uniqueId = GetUniqueId(type);
		_data.Add(value);
		_dataByUniqueId.Add(GetUniqueId(type), value);
		created = true;
		return value;
	}
}
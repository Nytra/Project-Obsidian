using FrooxEngine;

namespace Obsidian;

public readonly struct TypeDataResult
{
    public readonly TypeData data;

    public readonly DataFeedItemChange change;

    public TypeDataResult(TypeData data, DataFeedItemChange change)
    {
        this.data = data;
        this.change = change;
    }
}
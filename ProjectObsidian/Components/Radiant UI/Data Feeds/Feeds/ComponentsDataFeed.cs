﻿using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;

namespace Obsidian;

// This feed returns the components from the component library, which includes the categories (DataFeedCategoryItem)

[Category(new string[] { "Obsidian/Radiant UI/Data Feeds/Feeds" })]
public class ComponentsDataFeed : Component, IDataFeedComponent, IDataFeed, IWorldElement
{
    private Dictionary<SearchPhraseFeedUpdateHandler, ComponentsDataFeedData> _updateHandlers = new Dictionary<SearchPhraseFeedUpdateHandler, ComponentsDataFeedData>();

    public bool SupportsBackgroundQuerying => true;

    private static HashSet<Type> _componentTypes = new();

    private static bool SearchStringValid(string str)
    {
        return !string.IsNullOrWhiteSpace(str) && str.Length >= 3;
    }

    private void Update()
    {
        foreach (KeyValuePair<SearchPhraseFeedUpdateHandler, ComponentsDataFeedData> updateHandler in _updateHandlers)
        {
            updateHandler.Key.handler(null, DataFeedItemChange.PathItemsInvalidated);
        }
    }

    protected override void OnChanges()
    {
        Update();
    }

    private void GetAllTypes(HashSet<Type> allTypes, CategoryNode<Type> categoryNode)
    {
        foreach (var elem in categoryNode.Elements)
        {
            allTypes.Add(elem);
        }
        foreach (var subCat in categoryNode.Subcategories)
        {
            GetAllTypes(allTypes, subCat);
        }
    }

    private IEnumerable<Type> EnumerateAllTypes(CategoryNode<Type> categoryNode)
    {
        foreach (var elem in categoryNode.Elements)
        {
            yield return elem;
        }
        foreach (var subCat in categoryNode.Subcategories)
        {
            foreach(var elem2 in EnumerateAllTypes(subCat))
            {
                yield return elem2;
            }
        }
    }

    private string GetCategoryKey(CategoryNode<Type> categoryNode)
    {
        return categoryNode.Name;
    }

    private DataFeedCategory GenerateCategory(string key, IReadOnlyList<string> path)
    {
        DataFeedCategory dataFeedCategory = new DataFeedCategory();
        // random icon
        dataFeedCategory.InitBase(key, path, null, key, OfficialAssets.Graphics.Icons.Gizmo.TransformLocal);
        return dataFeedCategory;
    }

    private TypeFeedItem GenerateType(Type type, string key, IReadOnlyList<string> path)
    {
        TypeFeedItem typeFeedItem = new TypeFeedItem(type);
        // random icon
        typeFeedItem.InitBase(key, path, null, type.GetNiceName(), OfficialAssets.Graphics.Icons.Gizmo.TransformLocal);
        return typeFeedItem;
    }

    public async IAsyncEnumerable<DataFeedItem> Enumerate(IReadOnlyList<string> path, IReadOnlyList<string> groupKeys, string searchPhrase, object viewData)
    {
        if (groupKeys != null && groupKeys.Count > 0)
        {
            yield break;
        }

        ComponentsDataFeedData componentDataFeedData = (ComponentsDataFeedData)viewData;
        componentDataFeedData.Clear();
        searchPhrase = searchPhrase?.Trim();

        var lib = WorkerInitializer.ComponentLibrary;
        if (path != null && path.Count > 0)
        {
            var catNode = lib;
            foreach (var str in path)
            {
                var subCat = catNode.Subcategories.FirstOrDefault(x => x.Name == str);
                if (subCat != null)
                {
                    catNode = subCat;
                }
                else
                {
                    yield break;
                }
            }
            foreach (var subCat2 in catNode.Subcategories)
            {
                yield return GenerateCategory(GetCategoryKey(subCat2), path);
            }
            if (SearchStringValid(searchPhrase))
            {
                foreach (var elem in EnumerateAllTypes(catNode))
                {
                    componentDataFeedData.AddComponentType(elem);
                }
            }
            else
            {
                foreach (var elem in catNode.Elements)
                {
                    componentDataFeedData.AddComponentType(elem);
                }
            }
        }
        else
        {
            if (_componentTypes.Count == 0)
            {
                GetAllTypes(_componentTypes, lib);
            }
            foreach (var subCat in lib.Subcategories)
            {
                yield return GenerateCategory(GetCategoryKey(subCat), path);
            }
            if (SearchStringValid(searchPhrase))
            {
                foreach (var elem in _componentTypes)
                {
                    componentDataFeedData.AddComponentType(elem);
                }
            }
        }

        List<string> optionalTerms = Pool.BorrowList<string>();
        List<string> requiredTerms = Pool.BorrowList<string>();
        List<string> excludedTerms = Pool.BorrowList<string>();
        SearchQueryParser.Parse(searchPhrase, optionalTerms, requiredTerms, excludedTerms);
        foreach (TypeData componentData in componentDataFeedData.ComponentData)
        {
            if (componentData.MatchesSearchParameters(optionalTerms, requiredTerms, excludedTerms))
            {
                componentData.MarkSubmitted();
                yield return GenerateType(componentData.ComponentType, componentData.ComponentType.GetHashCode().ToString(), path);
            }
        }
        Pool.Return(ref optionalTerms);
        Pool.Return(ref requiredTerms);
        Pool.Return(ref excludedTerms);
    }

    public void ListenToUpdates(IReadOnlyList<string> path, IReadOnlyList<string> groupKeys, string searchPhrase, DataFeedUpdateHandler handler, object viewData)
    {
        if ((path == null || path.Count <= 0) && (groupKeys == null || groupKeys.Count <= 0))
        {
            var data = (ComponentsDataFeedData)viewData;
            _updateHandlers.Add(new SearchPhraseFeedUpdateHandler(handler, searchPhrase?.Trim()), data);
        }
    }

    public void UnregisterListener(IReadOnlyList<string> path, IReadOnlyList<string> groupKeys, string searchPhrase, DataFeedUpdateHandler handler)
    {
        if ((path == null || path.Count <= 0) && (groupKeys == null || groupKeys.Count <= 0))
        {
            _updateHandlers.Remove(new SearchPhraseFeedUpdateHandler(handler, searchPhrase?.Trim()));
        }
    }

    public LocaleString PathSegmentName(string segment, int depth)
    {
        return null;
    }

    public object RegisterViewData()
    {
        return new ComponentsDataFeedData();
    }

    public void UnregisterViewData(object data)
    {
    }

    protected override void OnDispose()
    {
        _updateHandlers.Clear();
        base.OnDispose();
    }
}
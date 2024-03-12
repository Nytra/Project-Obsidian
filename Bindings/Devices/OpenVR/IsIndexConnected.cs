// <auto-generated/>
/* *******************************
Generated for type: OpenvrDataGetter.IsIndexConnected
Generated on: 3/12/2024 2:10:51 AM
Source assembly: Project-Obsidian, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
******************************* */

using System;
using ProtoFlux.Core;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using OpenvrDataGetter;

namespace FrooxEngine.OpenvrDataGetter
{
    
    
    
    [Category("ProtoFlux/Runtimes/Execution/Nodes/OpenvrDataGetter")]
    public  partial class IsIndexConnected : global::FrooxEngine.OpenvrDataGetter.Nodes.TrackedDeviceData<global::System.Boolean>
        
        
    {
        public override Type NodeType => typeof(global::OpenvrDataGetter.IsIndexConnected);

        

        
public global::OpenvrDataGetter.IsIndexConnected TypedNodeInstance { get; private set;}
public override INode NodeInstance => TypedNodeInstance;

public override N Instantiate<N>()
{
                if(TypedNodeInstance != null)
                    throw new InvalidOperationException("Node has already been instantiated");

                var node = new global::OpenvrDataGetter.IsIndexConnected();

                TypedNodeInstance = node;

                OnInstantiated();

                return node as N;
}

partial void OnInstantiated();

protected override void AssociateInstanceInternal(INode node)
{
    if(node is global::OpenvrDataGetter.IsIndexConnected typedNode)
    {
        TypedNodeInstance = typedNode;

        OnInstantiated();
    }
    else
        throw new ArgumentException("Node instance is not of type " + typeof(global::OpenvrDataGetter.IsIndexConnected));
}

public override void ClearInstance() => TypedNodeInstance = null;


        
        
        

        
        

        
        

        
        

        
        

        

        
        

        
        
        
        

        

        
        
        
        

        
        

        


    }
}

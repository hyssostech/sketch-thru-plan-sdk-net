using System.Collections.Generic;
using System.ComponentModel;

namespace StpSDK;

public class StpNode<T> where T : StpItem, INotifyPropertyChanged
{
    public T Item { get; set; }
    public string Key { get; set; }
    public string ParentKey { get; set; }
    public string Description { get; set; }
    public int Depth { get; set; }
    public int ChildrenCount { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }

    public StpNode() { }

    public StpNode(T item, string uniqueKey, string parentId, string description = null)
    {
        Item = item;
        Key = uniqueKey;
        ParentKey = parentId;
        Description = description ?? Item.Description;
        Depth = 0;
        ChildrenCount = 0;
    }

    public override bool Equals(object obj)
    {
        return obj is StpNode<T> node &&
               Key == node.Key &&
               ParentKey == node.ParentKey &&
               Description == node.Description &&
               Depth == node.Depth &&
               ChildrenCount == node.ChildrenCount &&
               EqualityComparer<T>.Default.Equals(Item, node.Item);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash * 16777619) ^ Item.Poid.GetHashCode();
            hash = (hash * 16777619) ^ ParentKey.GetHashCode();
            return hash;
        }
    }
}

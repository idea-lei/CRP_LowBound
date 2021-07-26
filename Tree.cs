using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TreeNode<T> {
    public T Body;
    public List<T> Children;
    public TreeNode<T> Parent;

    public TreeNode(T body, TreeNode<T> parent) {
        Body = body;
        Parent = parent;
        Children = new List<T>();
        if (parent != null) parent.Children.Add(Body);
    }
}

public class NodeInfo {
    public Bay Bay;
    public Relocation RelocationFromParent;
    public NodeInfo(List<Container2D>[] l, Relocation r) {
        Bay = new Bay(l);
        RelocationFromParent = r;
    }
}

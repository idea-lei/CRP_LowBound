using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program {
    static void Main(string[] args) {
        var agent = new Bay2DAgent();
        agent.ResetEnv();
    }
}

public static class Parameters {
    public static int DimZ = 3;
    public static int MaxLayer = 3;
}

public class Bay2DAgent {
    public int count = 0;

    public Bay bay;
    public int maxLabel = (Parameters.DimZ - 1) * Parameters.MaxLayer + 1;

    public void ResetEnv() {

        if (count++ >= 1) {
            Evaluation.Print();
            return;
        }

        bay = new Bay(Parameters.DimZ, Parameters.MaxLayer, maxLabel);
        Console.WriteLine(bay);
        Simulate();
    }

    public void Simulate() {
        var resultRelocationQueue = new Queue<Relocation>();
        var b = new Bay(bay.Layout);
        Console.WriteLine(b);
        var topNode = new TreeNode<NodeInfo>(new NodeInfo(b.Layout, null), null);
        var resultNodes = new Stack<NodeInfo>();
        TraverseTree(topNode, ref resultNodes);
        while(resultNodes.Count >0) {
            resultRelocationQueue.Enqueue(resultNodes.Pop().RelocationFromParent);
        }
        Console.WriteLine(resultRelocationQueue.Count);
    }

    public void TraverseTree(TreeNode<NodeInfo> node, ref Stack<NodeInfo> resultNodes) {
        var b = new Bay(node.Body.Bay.Layout);
        while (b.canRetrieve) {
            b.retrieve();
        }
        if (b.empty) {
            var nodes = new Stack<NodeInfo>();
            TreeNode<NodeInfo> pNode = node;
            while (pNode != null) {
                nodes.Push(pNode.Body);
            }
            if (resultNodes.Count == 0 || resultNodes.Count > nodes.Count) resultNodes = nodes;
        }

        for (int z0 = 0; z0 < b.DimZ; z0++) {
            for (int z1 = 0; z1 < b.DimZ; z1++) {
                if (!b.canRelocate(z0, z1).Item1) continue;
                var newB = new Bay(b.Layout);
                var r = new Relocation(z0, z1);
                newB.relocate(r);

                TreeNode<NodeInfo> pNode = node;
                while (pNode != null) {
                    // if repeat layout
                    if (pNode.Body.Bay == newB) break;
                    pNode = pNode.Parent;
                }
                if (pNode != null) continue;

                var child = new TreeNode<NodeInfo>(new NodeInfo(newB.Layout, r), node);
                TraverseTree(child, ref resultNodes);
            }
        }
    }

    //public void TraverseTree(TreeNode<NodeInfo> node, ) {
    //    if (node.Children.Count == 0) {
    //        var pNode = node;
    //        var stack = new Stack<NodeInfo>();
    //        while (pNode != null) {
    //            stack.Push(pNode.Body);
    //        }
    //        if (resultNodes.Count == 0 || resultNodes.Count > stack.Count) resultNodes = stack;
    //    }
    //    foreach (var n in node.Children) {

    //    }
    //}


    //public async Task recurse(Bay b, Queue<Relocation> resultRelocationQueue, string callerName) {
    //    // if the current relocations already exceeds the min result in the candidates
    //    if (resultRelocationQueue.Any() && b.RelocationQueue.Count > resultRelocationQueue.Count) return;

    //    while (b.canRetrieve) {
    //        b.retrieve();
    //        b.lastOperation = new LastOperation();
    //    }
    //    if (b.empty) {
    //        resultRelocationQueue = new Queue<Relocation>(b.RelocationQueue);
    //        return;
    //    }

    //    if (b.RelocationQueue.Count > b.MaxLabel) return;

    //    for (int z0 = 0; z0 < b.DimZ; z0++) {
    //        for (int z1 = 0; z1 < b.DimZ; z1++) {
    //            if (!b.canRelocate(z0, z1).Item1) continue;
    //            if (b.lastOperation.z0 == z1 && b.lastOperation.z1 == z0) continue;
    //            var newB = new Bay(b.Layout, b.RelocationQueue, new LastOperation(_0: z0, _1: z1));
    //            var r = new Relocation(z0, z1);
    //            newB.relocate(r);
    //            newB.RelocationQueue.Enqueue(r);
    //            await recurse(newB, resultRelocationQueue, nameof(recurse));
    //        }
    //    }
    //    if (callerName == nameof(Simulate)) {
    //        Evaluation.UpdateValue(b.MaxLabel, resultRelocationQueue.Count);
    //        ResetEnv();
    //    }
    //}
}



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program {
    static void Main(string[] args) {
        var ts = new List<Task>();
        for (int _ = 0; _ < 70; _++) {
            var task = Task.Factory.StartNew(initAgent);
            ts.Add(task);
        }
        Task.WaitAll(ts.ToArray());
        Evaluation.Print();
    }



    static void initAgent() {
        var agent = new Bay2DAgent();
        agent.ResetEnv();
    }
}

public static class Parameters {
    public static int DimZ = 4;
    public static int MaxLayer = 4;
}

public class Bay2DAgent {
    public int count = 0;

    public Bay bay;
    public int maxLabel = (Parameters.DimZ - 1) * Parameters.MaxLayer + 1;
    public int instanceNum;
    public Stack<Relocation> relocations;

    public async void ResetEnv() {

        while (count++ < 5) {
            relocations = null;
            bay = new Bay(Parameters.DimZ, Parameters.MaxLayer, maxLabel);
            Console.WriteLine(bay);
            await Simulate();
            Console.WriteLine("instance finish");
        }
    }

    public async Task Simulate() {
        var resultRelocationQueue = new Queue<Relocation>();
        var b = new Bay(bay.Layout);
        var topNode = new TreeNode<NodeInfo>(new NodeInfo(b.Layout, null), null);
        var start = DateTime.Now;
        await Traverse(topNode);
        while (relocations.Count > 0) {
            resultRelocationQueue.Enqueue(relocations.Pop());
        }
        var end = DateTime.Now;
        Evaluation.UpdateValue(b.MaxLabel, resultRelocationQueue.Count, end - start);
    }

    public async Task Traverse(TreeNode<NodeInfo> node) {
        var b = new Bay(node.Body.Bay.Layout);
        while (b.canRetrieve) {
            b.retrieve();
        }
        if (b.empty) {
            var rs = new Stack<Relocation>();
            TreeNode<NodeInfo> pNode = node;
            while (pNode != null) {
                if (pNode.Body.RelocationFromParent != null)
                    rs.Push(pNode.Body.RelocationFromParent);
                pNode = pNode.Parent;
            }
            if (relocations == null || relocations.Count > rs.Count) relocations = rs;
        }

        // check exceeds max recursion times
        int ps = 0;
        var p = node.Parent;
        while (p != null) {
            if (p.Body.Bay == b) return;
            p = p.Parent;
            ps++;
            if (relocations != null && relocations.Count > 0 && ps > relocations.Count + 1) return;
        }
        if (ps > b.MaxLabel) return;

        for (int z0 = 0; z0 < b.DimZ; z0++) {
            for (int z1 = 0; z1 < b.DimZ; z1++) {
                if (!b.canRelocate(z0, z1).Item1) continue;
                if (b.Layout[z0].Count == 1 && b.IndexEmpty(z1)) continue;
                var newB = new Bay(b.Layout);
                var r = new Relocation(z0, z1);
                newB.relocate(r);

                var child = new TreeNode<NodeInfo>(new NodeInfo(newB.Layout, r), node);
                await Traverse(child);
            }
        }
    }
}

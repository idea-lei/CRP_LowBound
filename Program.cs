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
    int count = 0;

    Bay bay;
    int maxLabel = (Parameters.DimZ - 1) * Parameters.MaxLayer + 1;

    public void ResetEnv() {

        if (count++ >= 1) {
            Evaluation.Print();
            return;
        }

        bay = new Bay(Parameters.DimZ, Parameters.MaxLayer, maxLabel);
        Simulate();
    }

    public async void Simulate() {
        var resultRelocationQueue = new Queue<Relocation>();
        var b = new Bay(bay.Layout, new Queue<Relocation>(), new LastOperation());
        Console.WriteLine(b);
        await recurse(b, resultRelocationQueue, nameof(Simulate));
    }

    public async Task recurse(Bay b, Queue<Relocation> resultRelocationQueue, string callerName) {
        // if the current relocations already exceeds the min result in the candidates
        if (resultRelocationQueue.Any() && b.RelocationQueue.Count > resultRelocationQueue.Count) return;

        while (b.canRetrieve) {
            b.retrieve();
            b.lastOperation = new LastOperation();
        }
        if (b.empty) {
            resultRelocationQueue = new Queue<Relocation>(b.RelocationQueue);
            return;
        }

        for (int z0 = 0; z0 < b.DimZ; z0++) {
            for (int z1 = 0; z1 < b.DimZ; z1++) {
                if (!b.canRelocate(z0, z1).Item1) continue;
                if (b.lastOperation.z0 == z1 && b.lastOperation.z1 == z0) continue;
                var newB = new Bay(b.Layout, b.RelocationQueue, new LastOperation(_0: z0, _1: z1));
                var r = new Relocation(z0, z1);
                newB.relocate(r);
                newB.RelocationQueue.Enqueue(r);
                await recurse(newB, resultRelocationQueue, nameof(recurse));
            }
        }
        if (callerName == nameof(Simulate)) {
            Evaluation.UpdateValue(b.MaxLabel, resultRelocationQueue.Count);
            ResetEnv();
        }
    }
}

public static class Evaluation {
    private static int TotalContainerOut = 0;
    private static int TotalRelocation = 0;
    public static void UpdateValue(int o, int r) {
        TotalContainerOut += o;
        TotalRelocation += r;
    }

    public static void Print() {
        Console.WriteLine($"TotalContainerOut: {TotalContainerOut}, TotalRelocation: {TotalRelocation}");
    }
}








public struct Relocation {
    public int z0;
    public int z1;
    public Relocation(int _z0, int _z1) {
        z0 = _z0;
        z1 = _z1;
    }
}


public class Container2D : IComparable {
    public readonly int priority;
    public int relocationTimes;

    public Container2D(int p) {
        relocationTimes = 0;
        priority = p;
    }

    public static bool operator >(Container2D a, Container2D b) {
        return a.priority > b.priority;
    }

    public static bool operator <(Container2D a, Container2D b) {
        return a.priority < b.priority;
    }

    public static bool operator ==(Container2D a, Container2D b) {
        if (a is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Container2D a, Container2D b) {
        return !a.Equals(b);
    }

    public int CompareTo(object obj) {
        if (obj is Container2D c) {
            if (priority > c.priority) return 1;
            if (priority == c.priority) return 0;
            else return -1;
        }
        throw new Exception("other is not Container2D");
    }

    public override bool Equals(object obj) {
        if (obj is Container2D c) {
            return GetHashCode() == c.GetHashCode();
        }
        return false;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    public override string ToString() {
        return priority.ToString();
    }
}

public class Bay {
    private Stack<Container2D>[] layout; //[z,t]
    public readonly int MaxTier;
    public readonly int DimZ;

    private int maxLabel;
    public int MaxLabel => maxLabel;
    public LastOperation lastOperation;

    public Queue<Relocation> RelocationQueue;

    /// <summary>
    /// this property is for observation, the first element is stack top, last is bottom
    /// </summary>
    public List<Container2D>[] Layout {
        get {
            var list = new List<Container2D>[DimZ];
            for (int i = 0; i < DimZ; i++) {
                list[i] = layout[i].ToList();
            }
            return list;
        }
    }

    public Container2D[,] LayoutAs2DArray {
        get {
            var res = new Container2D[DimZ, MaxTier];
            var layout = Layout;
            for (int z = 0; z < DimZ; z++) {
                for (int t = 0; t < MaxTier; t++) {
                    res[z, t] = t < layout[z].Count ? layout[z][t] : null;
                }
            }
            return res;
        }
    }

    public int[] BlockingDegrees => layout.Select(s => BlockingDegree(s)).ToArray();

    public bool empty {
        get {
            foreach (var s in layout) {
                if (s.Count > 0) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Item1: Item, 
    /// Item2: z-index
    /// </summary>
    public (Container2D, int) min {
        get {
            Container2D m = new Container2D(int.MaxValue);
            int index = 0;
            for (int i = 0; i < layout.Length; i++) {
                if (layout[i].Count > 0) {
                    var _m = layout[i].Min();
                    if (m > _m) {
                        m = _m;
                        index = i;
                    }
                }
            }
            return (m, index);
        }
    }

    public Bay(int z, int t, int _maxLabel) {
        layout = new Stack<Container2D>[z];
        for (int i = 0; i < z; i++) {
            layout[i] = new Stack<Container2D>();
        }
        MaxTier = t;
        DimZ = z;
        maxLabel = _maxLabel;
        assignValues(generateSequence(_maxLabel));
        RelocationQueue = new Queue<Relocation>();
    }

    public Bay(List<Container2D>[] _layout, Queue<Relocation> rQueue, LastOperation last) {
        RelocationQueue = new Queue<Relocation>(rQueue);
        DimZ = _layout.Length;
        maxLabel = _layout.Max(l => l.Count > 0 ? l.Max(c => c.priority) : 0);
        MaxTier = (maxLabel - 1) / (DimZ - 1);
        lastOperation = last;

        layout = new Stack<Container2D>[DimZ];
        for (int z = 0; z < DimZ; z++) {
            layout[z] = new Stack<Container2D>();
            foreach (var c in _layout[z]) stack(z, c);
        }
    }

    public bool CheckDim() {
        return Layout.All(s => s.Count <= MaxTier);
    }

    public bool IndexFull(int z) {
        return layout[z].Count >= DimZ;
    }

    public bool IndexEmpty(int z) {
        return layout[z].Count <= 0;
    }

    /// <param name="z0">pick up pos</param>
    /// <param name="z1">stack pos</param>
    /// <returns>true if can relocate, the Item2 is reason--> 0: z0 empty, 1: z1 full, 2: both, 3: same index, 4: success</returns>

    public (bool, int) canRelocate(int z0, int z1) {
        (bool, int) res = (true, 4);
        if (IndexEmpty(z0)) res = (false, 0);
        if (IndexFull(z1)) res = (false, 1);
        if (IndexEmpty(z0) && IndexFull(z1)) res = (false, 2);
        if (z0 == z1) res = (false, 3);
        return res;
    }

    public (bool, int) canRelocate(Relocation r) => canRelocate(r.z0, r.z1);

    // check canRelocate first
    public Container2D relocate(int z0, int z1) {
        var c = layout[z0].Pop();
        c.relocationTimes++;
        layout[z1].Push(c);
        return c;
    }

    public Container2D relocate(Relocation r) => relocate(r.z0, r.z1);

    public bool stack(int z, Container2D v) {
        if (layout[z].Count == MaxTier) return false;
        layout[z].Push(v);
        return true;
    }

    public bool canRetrieve {
        get {
            if (empty) return false;
            var m = min;
            return layout[m.Item2].Peek() == m.Item1;
        }
    }

    /// <summary>
    /// check canRetrieve first!
    /// </summary>
    /// <returns> relocation times of this container</returns>
    public int retrieve() {
        var c = layout[min.Item2].Pop();
        return c.relocationTimes;
    }

    private int[] generateSequence(int i) {
        System.Random random = new System.Random();
        return Enumerable.Range(1, i).OrderBy(x => random.Next()).ToArray();
    }

    private void assignValues(int[] arr) {
        int i = 0;
        while (i < arr.Length) {
            var rnd = new Random();
            int z = rnd.Next(DimZ);
            if (layout[z].Count >= MaxTier) continue;
            if (stack(z, new Container2D(arr[i]))) i++;
        }
    }

    // peak value of z-index
    public Container2D Peek(int z) {
        return layout[z].Peek();
    }


    // from https://iopscience.iop.org/article/10.1088/1742-6596/1873/1/012050/pdf
    public int BlockingDegree(Stack<Container2D> s) {
        int degree = 0;
        var list = s.Select(c => c.priority).ToList();
        list.Reverse();

        List<int> hList;
        while (list.Count > 1) {
            int truncate = list.IndexOf(list.Min());
            hList = list.GetRange(truncate, list.Count - truncate);
            if (hList.Count > 1) foreach (int x in hList) degree += hList[0] - x;
            list = list.GetRange(0, truncate);
        }

        return degree;
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        foreach (var s in layout) {
            var list = s.ToList();
            list.Reverse();
            sb.Append(string.Join(", ", list) + "\n");
        }
        return sb.ToString();
    }
}

public struct LastOperation {
    public readonly bool success;
    public readonly int z0; // pick up pos
    public readonly int z1; // stack pos
    public readonly int repeatTimes;

    public LastOperation(bool s = true, int _0 = -1, int _1 = -1, int r = 0) {
        success = s;
        z0 = _0;
        z1 = _1;
        repeatTimes = r;
    }

    public static bool operator ==(LastOperation a, LastOperation b) {
        return a.success == b.success && a.z0 == b.z0 && a.z1 == b.z1;
    }

    public static bool operator !=(LastOperation a, LastOperation b) {
        return !(a == b);
    }

    public override bool Equals(object obj) {
        if (obj is LastOperation c) {
            return GetHashCode() == c.GetHashCode();
        }
        return false;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }
}
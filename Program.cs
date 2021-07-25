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

    public async void Simulate() {
        var resultRelocationQueue = new Queue<Relocation>();
        var b = new Bay(bay.Layout, new Queue<Relocation>(), new LastOperation());

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

        if (b.RelocationQueue.Count > b.MaxLabel) return;

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



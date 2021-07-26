using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

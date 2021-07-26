using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Evaluation {
    private static int TotalContainerOut = 0;
    private static int TotalRelocation = 0;
    private static TimeSpan TotalTime = new TimeSpan();
    public static void UpdateValue(int containerOut, int relocations, TimeSpan timespan) {
        TotalContainerOut += containerOut;
        TotalRelocation += relocations;
        TotalTime += timespan;
    }

    public static void Print() {
        Console.WriteLine($"TotalContainerOut: {TotalContainerOut}\n" +
            $"TotalRelocation: {TotalRelocation}\n" +
            $"RelocationRate: {TotalRelocation/ (float)TotalContainerOut}\n" +
            $"TotalTime: {TotalTime.TotalSeconds}s\n");
    }
}

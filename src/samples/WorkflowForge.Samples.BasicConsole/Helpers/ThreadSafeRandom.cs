namespace WorkflowForge.Samples.BasicConsole.Helpers;

/// <summary>
/// Cross-framework thread-safe random number generator.
/// On .NET 6+, delegates to <see cref="Random.Shared"/> (zero overhead).
/// On .NET Framework 4.8, uses a [ThreadStatic] local seeded from a locked global
/// to avoid the TickCount-collision problem where rapid <c>new Random()</c> calls
/// within the same millisecond produce identical sequences.
/// </summary>
internal static class ThreadSafeRandom
{
#if NET6_0_OR_GREATER
    public static int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
    public static int Next(int maxValue) => Random.Shared.Next(maxValue);
    public static double NextDouble() => Random.Shared.NextDouble();
    public static void NextBytes(byte[] buffer) => Random.Shared.NextBytes(buffer);
#else
    private static readonly Random s_global = new Random();
    [ThreadStatic] private static Random? t_local;

    private static Random GetLocal()
    {
        if (t_local == null)
        {
            int seed;
            lock (s_global) { seed = s_global.Next(); }
            t_local = new Random(seed);
        }
        return t_local;
    }

    public static int Next(int minValue, int maxValue) => GetLocal().Next(minValue, maxValue);

    public static int Next(int maxValue) => GetLocal().Next(maxValue);

    public static double NextDouble() => GetLocal().NextDouble();

    public static void NextBytes(byte[] buffer) => GetLocal().NextBytes(buffer);

#endif
}
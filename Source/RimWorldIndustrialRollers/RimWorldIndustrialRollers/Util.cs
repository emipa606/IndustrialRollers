using System;

namespace RimWorldIndustrialRollers;

internal class Util
{
    public const bool DEBUG = false;

    private static readonly Random Random = new Random();

    private static readonly object randLock = new object();

    public static void Log(string message)
    {
    }

    public static int RandomBetween(int min, int max)
    {
        lock (randLock)
        {
            return Random.Next(min, max + 1);
        }
    }
}
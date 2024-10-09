using System;

namespace TownOfHost;
class Event
{
    public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
    public static bool White = DateTime.Now.Month == 3 && DateTime.Now.Day is 14;
    public static bool IsInitialRelease = DateTime.Now.Month == 10 && DateTime.Now.Day is 31;
    public static bool IsHalloween = DateTime.Now.Month == 10 && DateTime.Now.Day is 31;
    public static bool GoldenWeek = DateTime.Now.Month == 5 && DateTime.Now.Day is 3 or 4 or 5;
    public static bool April = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public static bool IsEventDay => IsChristmas || White || IsInitialRelease || IsHalloween || GoldenWeek || April;
}
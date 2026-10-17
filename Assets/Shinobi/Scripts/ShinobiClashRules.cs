using System;

namespace Shinobi
{
    public enum ShinobiClashResult { Draw, FirstWins, SecondWins, Trade }
    public static class ShinobiClashRules
    {
        public static float Chance(int difference, float initial, float perLevel, float maximum)
        {
            if (difference <= 0) return 0f;
            return Math.Max(0f, Math.Min(100f, Math.Min(maximum,
                initial + (difference - 1) * Math.Max(0f, perLevel))));
        }
        public static ShinobiClashResult Resolve(int first, int second, float chance, float roll)
        {
            if (first == second) return ShinobiClashResult.Draw;
            if (roll >= chance) return ShinobiClashResult.Trade;
            return first > second ? ShinobiClashResult.FirstWins : ShinobiClashResult.SecondWins;
        }
    }
}

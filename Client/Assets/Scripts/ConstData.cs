using System;

public static class ConstData
{
    public static readonly double MoneyAimTime = TimeSpan.FromMinutes(1).TotalSeconds;

    private static readonly int[] cardbackPrices =
        { 50, 65, 120, 450, 800, 1100, 2000, 3000, 5000 };
    private static readonly int[] backgroundPrices =
        { 50, 65, 300, 600, 1000, 2100, 3050, 3900, 6000, 9000 };

    public static int[][] ItemPrices => new[] { cardbackPrices, backgroundPrices };
}
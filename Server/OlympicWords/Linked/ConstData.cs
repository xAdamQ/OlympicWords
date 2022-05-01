using System;

public static class ConstData
{
    public static readonly double MoneyAimTime = TimeSpan.FromMinutes(1).TotalSeconds;

    private static int[] cardbackPrices = {50, 65, 100, 450, 600, 700, 1800, 2000, 2600};
    private static int[] backgroundPrices = {50, 65, 100, 450, 600,};
    public static int[][] ItemPrices => new[] {cardbackPrices, backgroundPrices};
}
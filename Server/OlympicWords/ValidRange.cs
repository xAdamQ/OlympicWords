namespace OlympicWords;
[AttributeUsage(AttributeTargets.Parameter)]
public class ValidRange : Attribute
{
    //these can use generic math but this is dotnet 6
    public int Min { get; }
    public int Max { get; }
    public ValidRange(int min, int max)
    {
        Min = min;
        Max = max;
    }
    public ValidRange(int max)
    {
        Max = max;
    }
}
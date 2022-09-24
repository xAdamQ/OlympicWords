using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using OlympicWords.Services;

namespace OlympicWords;

public class ParagraphManager
{
    private readonly Paragraph[] paragraphs;

    public ParagraphManager()
    {
        using var reader = new StreamReader("Paragraphs.csv");
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        paragraphs = csv.GetRecords<Paragraph>().ToArray();
    }

    public List<Paragraph> GetParagraphs(int count)
    {
        var res = new List<Paragraph>();
        for (int i = 0; i < count; i++)
        {
            var pIndex = StaticRandom.GetRandom(paragraphs.Length);
            res.Add(paragraphs[pIndex]);
        }

        return res;
    }
}

public class FooMap : ClassMap<Paragraph>
{
    public FooMap()
    {
    }
}
public class Paragraph
{
    public string
        Topic,
        Prompt;

    public string
        Sentence1,
        Sentence2,
        Sentence3,
        Sentence4,
        Sentence5,
        Sentence6,
        Sentence7,
        Sentence8,
        Sentence9,
        Sentence10;

    [Name("Paragraph ID")] public int Id;
}
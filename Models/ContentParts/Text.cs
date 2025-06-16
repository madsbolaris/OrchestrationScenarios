namespace OrchestrationScenarios.Models.ContentParts;

public class Text : ContentPart
{
    public string Value { get; set; }

    public Text(string value)
    {
        Value = value;
    }
}

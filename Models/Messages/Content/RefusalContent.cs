namespace OrchestrationScenarios.Models.Messages.Content;

public class RefusalContent : AIContent
{
    public override string Type => "refusal";

    public string Refusal { get; set; } = default!;
}

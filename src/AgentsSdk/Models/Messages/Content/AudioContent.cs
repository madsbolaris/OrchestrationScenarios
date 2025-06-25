namespace AgentsSdk.Models.Messages.Content;

public class AudioContent : FileContent
{
    public override string Type => "audio";

    public short? Duration { get; set; }
}

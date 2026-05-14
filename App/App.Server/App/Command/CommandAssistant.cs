public class CommandAssistant(CommandContext context, OpenAi openAi)
{
    public async Task<AssistantDto> Run(string text)
    {
        var resultText = await openAi.CompleteChatAsync(text);
        var result = new AssistantDto { Text = resultText };
        return result;
    }
}

public class AssistantDto
{
    public string? Text { get; set; }
}

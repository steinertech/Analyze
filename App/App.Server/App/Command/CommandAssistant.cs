public class CommandAssistant(AiService ai)
{
    public async Task<AssistantDto> Run(string text)
    {
        var resultText = await ai.CompleteChatAsync(text);
        var result = new AssistantDto { Text = resultText };
        return result;
    }
}

public class AssistantDto
{
    public string? Text { get; set; }
}

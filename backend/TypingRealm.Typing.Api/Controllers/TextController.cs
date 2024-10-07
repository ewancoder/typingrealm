using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TypingRealm.Typing.Api.Controllers;

// This controller will be moved to a separate project/service later, together with OpenAI functionality.
[Authorize]
[Route("api/text")]
public sealed class TextController : ControllerBase
{
    private const int MaxTextLength = 1000;
    private readonly OpenAIClient _openAiClient;

    public TextController(IConfiguration configuration)
    {
        var openAiKey = configuration["OpenAIKey"]
            ?? throw new InvalidOperationException("Could not read OpenAIKey.");

        _openAiClient = new OpenAIClient(openAiKey);
    }

    [HttpGet]
    [Route("generate")]
    public async ValueTask<ActionResult<object>> GenerateText(int length, string? theme, string? forTraining, string? language, bool? romanized)
    {
        _ = romanized;

        var isGlyph = false; // Disable glyphs for now.
        var hieroglyphPrompt = $"Generate {language ?? "japanese"} text, break it down into hieroglyphs and romanize each hieroglyph. Output only the following information in the following format: On each new line there should be a single one hieroglyph followed by a > character and then the romanized version of that single particular hieroglyph. Romanized version of the text should be at least 300 characters long.";

        if (length <= 10)
            return BadRequest($"Length should be at least 10 characters long.");

        if (length > MaxTextLength)
            return BadRequest($"Length should not exceed {MaxTextLength} characters.");

        if (theme?.Length > 100)
            theme = theme[..100];

        var themeString = theme?.Length > 0
            ? $", theme based on '{theme}'"
            : ", random theme of your choosing";

        var languagePrompt = language == null ? string.Empty : $", in {language} language";

        _ = themeString;
        _ = languagePrompt;

        var options = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                //new ChatRequestSystemMessage($"Generate text without any ambient info, {length} characters long, for training typing, decently spread out across whole keyboard{languagePrompt}"),
                new ChatRequestSystemMessage(hieroglyphPrompt)
                //new ChatRequestUserMessage($"Generate meaningful thematic text{themeString}")
            }
        };

        if (!isGlyph)
        {
            options = new ChatCompletionsOptions
            {
                DeploymentName = "gpt-3.5-turbo",
                Messages =
                {
                    new ChatRequestSystemMessage($"Generate text without any ambient info, {length} characters long, for training typing, decently spread out across whole keyboard{languagePrompt}"),
                    //new ChatRequestSystemMessage(hieroglyphPrompt)
                    new ChatRequestUserMessage($"Generate meaningful thematic text{themeString}")
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(forTraining))
        {
            options = new ChatCompletionsOptions
            {
                DeploymentName = "gpt-3.5-turbo",
                Messages =
                {
                    new ChatRequestSystemMessage($"Generate text without any ambient info, {length} characters long, for training typing the character {forTraining}"),
                    new ChatRequestUserMessage($"Generate meaningful text for training typing character '{forTraining}'")
                }
            };
        }

        var response = await _openAiClient.GetChatCompletionsAsync(options);
        var responseMessage = response.Value.Choices[0].Message;
        var content = responseMessage.Content;

        if (isGlyph)
        {
            return Ok(ParseGlyphs(content));
        }

        return responseMessage.Content[..Math.Min(responseMessage.Content.Length, length)];
    }

    private IEnumerable<object> ParseGlyphs(string content)
    {
        return content.Split('\n')
            .Where(x => x.Contains(">"))
            .Select(x => x.Split('>').Select(y => y.Trim()).ToArray())
            .Select(x => new
            {
                Glyph = x[0],
                Romanized = x[1]
            });
    }
}

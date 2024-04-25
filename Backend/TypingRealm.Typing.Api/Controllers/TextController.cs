using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

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
    public async ValueTask<ActionResult<string>> GenerateText(int length)
    {
        if (length <= 10)
            return BadRequest($"Length should be at least 10 characters long.");

        if (length > MaxTextLength)
            return BadRequest($"Length should not exceed {MaxTextLength} characters.");

        var options = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                new ChatRequestSystemMessage("Generate text without any ambient info"),
                new ChatRequestUserMessage($"Generate meaningful thematic text {length} characters long, to train typing across whole keyboard decently spread out")
            }
        };

        var response = await _openAiClient.GetChatCompletionsAsync(options);
        var responseMessage = response.Value.Choices[0].Message;
        return responseMessage.Content;
    }
}

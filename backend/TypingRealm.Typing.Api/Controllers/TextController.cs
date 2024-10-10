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
    private Random _random;

    public TextController(
        IConfiguration configuration,
        Random random)
    {
        _random = random;
        var openAiKey = configuration["OpenAIKey"]
            ?? throw new InvalidOperationException("Could not read OpenAIKey.");

        _openAiClient = new OpenAIClient(openAiKey);
    }

    [HttpGet]
    [Route("generate")]
    public async ValueTask<ActionResult<object>> GenerateText(int length, string? theme, string? forTraining, string? language, bool romanized = false)
    {
        var isGlyph = romanized;
        var hieroglyphPrompt = $"Generate {language ?? "japanese"} text, break it down into hieroglyphs and romanize each hieroglyph. Output only the following information in the following format: On each new line there should be a single one hieroglyph followed by a > character and then the romanized version of that single particular hieroglyph. Romanized version of the text should be at least 300 characters long.";

        if (length <= 10)
            return BadRequest($"Length should be at least 10 characters long.");

        if (length > MaxTextLength)
            return BadRequest($"Length should not exceed {MaxTextLength} characters.");

        if (theme?.Length > 100)
            theme = theme[..100];

        var themeString = theme?.Length > 0
            ? $", theme based on '{theme}'"
            : $", based on theme '{GetRandomTheme()}'";

        var languagePrompt = language == null ? string.Empty : $", in {language} language";

        _ = themeString;
        _ = languagePrompt;

        var options = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4o-mini",
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
                DeploymentName = "gpt-4o-mini",
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
                DeploymentName = "gpt-4o-mini",
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

    private string GetRandomTheme()
    {
        var topicIndex = _random.Next(0, _topics.Length);
        return _topics[topicIndex];
    }

    private static readonly string[] _topics =
    [
        "Space Exploration",
        "The Art of Woodcutting",
        "The History of Ancient Egypt",
        "Life in a Medieval Castle",
        "The Evolution of Technology",
        "The Wonders of the Ocean",
        "Robotics in Everyday Life",
        "The Psychology of Dreams",
        "Renewable Energy Sources",
        "The Art of Calligraphy",
        "The Science of Climate Change",
        "Time Travel Theories",
        "Life on Other Planets",
        "The Renaissance Era",
        "Artificial Intelligence in Medicine",
        "The Role of Social Media in Society",
        "The Rise and Fall of the Roman Empire",
        "History of the Internet",
        "The Art of Film Making",
        "Biodiversity in the Amazon Rainforest",
        "Cryptocurrency and Blockchain",
        "The Future of Self-Driving Cars",
        "The Development of Quantum Computing",
        "The World of Virtual Reality",
        "The Importance of Mental Health",
        "The History of Aviation",
        "Modern Architecture",
        "Astronomy and the Stars",
        "The Science of Genetics",
        "Cultural Significance of Tattoos",
        "The Impact of Globalization",
        "Cybersecurity in the Digital Age",
        "The Origins of the Universe",
        "Space Tourism",
        "The Role of Women in History",
        "The Industrial Revolution",
        "Animal Conservation Efforts",
        "The Power of Music Therapy",
        "The Marvels of Ancient Greece",
        "Smart Cities of the Future",
        "The Ethics of AI Development",
        "Human Evolution",
        "The Power of Storytelling",
        "The Mystery of the Bermuda Triangle",
        "Nanotechnology and its Applications",
        "The History of Video Games",
        "The Art of Animation",
        "The Significance of World Heritage Sites",
        "Sustainable Architecture",
        "The Influence of Pop Culture",
        "The Global Water Crisis",
        "Virtual Currencies",
        "Deep Sea Exploration",
        "The Future of Space Colonization",
        "The Role of Sports in Society",
        "Bioluminescent Creatures",
        "The Evolution of Dinosaurs",
        "Alternative Energy Innovations",
        "Space Junk and its Impact",
        "The History of Pirates",
        "The Science of Nutrition",
        "Exploring the Deep Web",
        "The Importance of Sleep",
        "Famous Historical Inventions",
        "The Magic of Illusionists",
        "3D Printing and Its Future",
        "The Role of the United Nations",
        "The History of Written Language",
        "The World of E-Sports",
        "The Impact of Fast Fashion",
        "The Study of Cryptography",
        "The Evolution of Fashion",
        "Climate Change Mitigation Strategies",
        "The History of Comic Books",
        "The Science of Memory",
        "Augmented Reality in Education",
        "The Wonders of National Parks",
        "The History of Space Probes",
        "The Influence of Jazz Music",
        "The Role of Drones in Agriculture",
        "Famous World Monuments",
        "The Story of Human Rights",
        "The Development of Wireless Communication",
        "Robots in Space Exploration",
        "Humanitarian Aid and Disaster Relief",
        "The World of Competitive Gaming",
        "The Influence of the Industrial Age",
        "The Science of Volcanoes",
        "Paleontology and Fossil Discoveries",
        "The Future of Genetic Engineering",
        "The Art of Public Speaking",
        "The History of the Olympic Games",
        "Famous Ancient Civilizations",
        "The Future of Renewable Energy",
        "Ocean Acidification",
        "Space Habitats and Living in Space",
        "Marine Conservation",
        "The Role of Education in Society",
        "The History of Transportation",
        "The Power of Meditation",
        "Artificial Intelligence in Art"
    ];
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TypingRealm.Game.DataAccess;

namespace TypingRealm.Game.Api.Controllers;

[Authorize]
[Route("api/characters")]
public class CharacterController : ControllerBase
{
    private readonly CharacterRepository _characterRepository;

    public CharacterController(CharacterRepository characterRepository)
    {
        _characterRepository = characterRepository;
    }

    [HttpGet]
    [Route("me")]
    public ValueTask<CharacterInfo> GetMe()
    {
        return _characterRepository.GetCurrentCharacterInfoAsync();
    }

    [HttpPost]
    public ValueTask CreateNew(CreateCharacterDto createCharacterDto)
    {
        return _characterRepository.CreateCharacterAsync(createCharacterDto);
    }
}

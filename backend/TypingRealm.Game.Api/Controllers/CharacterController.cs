using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TypingRealm.Game.Api.Controllers;

public record CharacterInfo(string Name, int Level, long Experience, long Wood, decimal AxeCondition);

[Authorize]
[Route("api/characters")]
public class CharacterController
{
    private long _wood = 10;
    private decimal _axeCondition = 100;

    [HttpGet]
    [Route("me")]
    public async ValueTask<CharacterInfo> GetMe(int errors = 0)
    {
        _wood += 2;
        _axeCondition -= 0.5m * errors;

        return new("Ivan", 80, 348_818_283_182_283_481, _wood, _axeCondition);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TypingRealm.Framework;

namespace TypingRealm.ApiHost;

public sealed class AuthenticationContext : IAuthenticationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetUserProfileId()
    {
        return _httpContextAccessor.HttpContext?.User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
            ?? throw new InvalidOperationException("User is not authenticated.");
    }
}


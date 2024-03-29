﻿using System.Threading;
using System.Threading.Tasks;
using TypingRealm.Messaging.Connecting;
using TypingRealm.Messaging.Messages;

namespace TypingRealm.World;

public sealed class ConnectHook : IConnectHook
{
    private readonly ILocationRepository _locationStore;

    public ConnectHook(ILocationRepository locationStore)
    {
        _locationStore = locationStore;
    }

    public ValueTask HandleAsync(Connect connect, CancellationToken cancellationToken)
    {
        var location = _locationStore.FindLocationForCharacter(connect.ClientId);
        if (location == null)
        {
            location = _locationStore.FindStartingLocation(connect.ClientId); // First time joining the World.
            location.AddCharacter(connect.ClientId);
            _locationStore.Save(location);
        }

        connect.Group = location.LocationId;
        return default;
    }
}

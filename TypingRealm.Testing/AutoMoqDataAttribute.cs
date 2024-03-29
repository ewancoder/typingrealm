﻿using System.IO;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using TypingRealm.Messaging;

namespace TypingRealm.Testing;

public sealed class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute() : base(() => CreateFixture()) { }
    public AutoMoqDataAttribute(bool configureMembers)
        : base(() => CreateFixture(configureMembers)) { }

    public static Fixture CreateFixture(bool configureMembers = true)
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = configureMembers });
        fixture.Register<Stream>(() => new MemoryStream());

        // TODO: Move out to Connections domain.
        fixture.Customize<ConnectedClient>(x => x.OmitAutoProperties());

        return fixture;
    }
}

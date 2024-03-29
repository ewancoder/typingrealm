﻿using System;
using System.Collections.Generic;
using System.Linq;
using TypingRealm.Profiles.Activities;

namespace TypingRealm.World;

public abstract class Activity
{
    protected Activity(string activityId, ActivityType type, string name, string creatorId)
    {
        ActivityId = activityId;
        Type = type;
        Name = name;
        CreatorId = creatorId;
    }

    public ActivityType Type { get; }
    public string ActivityId { get; }
    public string Name { get; }
    public string CreatorId { get; }

    public bool HasStarted { get; private set; }
    public bool HasFinished { get; private set; }
    public bool CanEdit => !HasStarted && !HasFinished;

    public bool IsInProgress => HasStarted && !HasFinished;

    public bool HasParticipant(string characterId) => GetParticipants().Contains(characterId);

    protected void Start()
    {
        if (HasStarted)
            throw new InvalidOperationException("Activity has already been started.");

        if (HasFinished)
            throw new InvalidOperationException("Activity has already been finished.");

        HasStarted = true;
    }

    /// <summary>
    /// Activity can be finished without starting - when we need to cancel it.
    /// </summary>
    protected void Finish()
    {
        if (HasFinished)
            throw new InvalidOperationException("Activity has already been finished.");

        HasFinished = true;
    }

    public abstract IEnumerable<string> GetParticipants();
}

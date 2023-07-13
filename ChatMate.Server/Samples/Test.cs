﻿using ChatMate.Abstractions.Model;

namespace ChatMate.Server.Samples;

public static class Test
{
    public static Character Create() => new()
    {
        Id = "c8e5ffe4-cda0-4e7f-953d-175d5e7164e7",
        ReadOnly = true,
        Name = "Test",
        CreatorNotes = "For testing",
        SystemPrompt = """
            This is a test system prompt
            Date and time: {{Now}}
            """,
        Scenario = "This is a test scenario between {{char}} and {{user}}",
        MessageExamples = """
            {{user}}: hello
            {{char}}: world
            """,
        Description = "test description",
        Personality = "test personality",
        PostHistoryInstructions = """
            Post history instructions of {{char}}. Current date and time: {{Now}}.
            """,
        FirstMessage = "Hello {{user}}! I'm {{char}}, a test character.",
        Services = new()
        {
            TextGen = new()
            {
                Service = "Fakes",
            },
            SpeechGen = new()
            {
                Service = "Fakes",
                Voice = "fake"
            }
        },
        Options = new()
        {
            EnableThinkingSpeech = false
        }
    };
}
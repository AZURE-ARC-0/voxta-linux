﻿using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface ITextGenService : ITokenCounter, IService
{
    int SummarizationTriggerTokens { get; }
    ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken);
    ValueTask<string > GenerateAsync(string prompt, CancellationToken cancellationToken);
}
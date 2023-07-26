﻿namespace Voxta.Abstractions.Services;

public interface IService : IDisposable
{
    string[] Features { get; }
    Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken);
}
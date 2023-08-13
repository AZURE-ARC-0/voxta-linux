using System.Collections.Concurrent;
using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public partial class ChatSession
{
    private readonly BlockingCollection<Func<CancellationToken, ValueTask>> _messageQueue = new();
    private readonly Task _messageQueueProcessTask;
    private readonly CancellationTokenSource _messageQueueCancellationTokenSource = new();
    private readonly SemaphoreSlim _processingSemaphore = new(0);

    private void Enqueue(Func<CancellationToken, ValueTask> fn)
    {
        try
        {
            _messageQueue.Add(fn, _messageQueueCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }
    
    private void EnqueueNow(Func<CancellationToken, ValueTask> fn)
    {
        try
        {
            var task = fn(_messageQueueCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var message in _messageQueue.GetConsumingEnumerable(cancellationToken))
            {
                _processingSemaphore.Release();
                try
                {
                    await message(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "Error processing message {MessageType}", message.GetType().Name);
                    await _tunnel.SendAsync(new ServerErrorMessage(exc), cancellationToken);
                }
                finally
                {
                    await _processingSemaphore.WaitAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _messageQueue.Dispose();
        }
    }

    public async Task WaitForPendingQueueItemsAsync()
    {
        try
        {
            while (_messageQueue.Count > 0 || _processingSemaphore.CurrentCount > 0)
            {
                await Task.Delay(10);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }
}

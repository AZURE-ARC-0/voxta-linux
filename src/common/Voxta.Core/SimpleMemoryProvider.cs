using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Core;

public class SimpleMemoryProvider : IMemoryProvider
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly List<(MemoryItem Source, ChatSessionDataMemory Data)> _memories = new();

    private Guid _lastProcessedMessageId = Guid.Empty;
    
    public SimpleMemoryProvider(IMemoryRepository memoryRepository, IPerformanceMetrics performanceMetrics)
    {
        _memoryRepository = memoryRepository;
        _performanceMetrics = performanceMetrics;
    }

    public async Task Initialize(Guid characterId, IChatTextProcessor textProcessor, CancellationToken cancellationToken)
    {
        var books = await _memoryRepository.GetScopeMemoryBooksAsync(characterId, cancellationToken);
        foreach (var book in books)
        {
            _memories.AddRange(book.Items.Select(x => (Source: x, Data: new ChatSessionDataMemory
            {
                Id = x.Id,
                Text = textProcessor.ProcessText(x.Text),
            })));
        }
    }
    
    public void QueryMemoryFast(ChatSessionData chat)
    {
        var perf = _performanceMetrics.Start("SimpleMemoryProvider");
        
        foreach (var msg in chat.GetMessages().SkipWhile(m => _lastProcessedMessageId != Guid.Empty && m.Id != _lastProcessedMessageId).Skip(_lastProcessedMessageId == Guid.Empty ? 0 : 1))
        {
            if (string.IsNullOrEmpty(msg.Value)) continue;
            foreach (var memory in _memories)
            {
                if (memory.Source.Keywords.Any(k => msg.Value.Contains(k, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var indexOfMemory = chat.Memories.FindIndex(m => m.Id == memory.Data.Id);
                    if (indexOfMemory == 0)
                        continue;
                    if (indexOfMemory > 0)
                        chat.Memories.RemoveAt(indexOfMemory);
                    chat.Memories.Insert(0, memory.Data);
                }
            }
            _lastProcessedMessageId = msg.Id;
        }

        perf.Done();
    }
}
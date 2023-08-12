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
        
        // Create words index
        #warning Improve perf, e.g. by keeping a set per message. Note that if messages are updated, we should clear the words.
        #warning Memory relevance should be higher for more recent messages
        #warning We can stop as soon as we have enough tokens instead of always doing them all
        #warning Do not re-process messages every time. Once processed, we're fine.
        foreach (var msg in chat.GetMessages())
        {
            if (string.IsNullOrEmpty(msg.Text)) continue;
            foreach (var memory in _memories)
            {
                if (memory.Source.Keywords.Any(k => msg.Text.Contains(k, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var indexOfMemory = chat.Memories.FindIndex(m => m.Id == memory.Data.Id);
                    if (indexOfMemory == 0)
                        continue;
                    if (indexOfMemory > 0)
                        chat.Memories.RemoveAt(indexOfMemory);
                    chat.Memories.Insert(0, memory.Data);
                }
            }
        }

        perf.Done();
    }
}
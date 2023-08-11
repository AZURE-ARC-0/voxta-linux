using System.Globalization;
using System.Text.RegularExpressions;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Core;

public class SimpleMemoryProvider : IMemoryProvider
{
    private static readonly Regex WordsRegex = new(@"[a-zA-Z]{2,}", RegexOptions.Compiled);

    private readonly IProfileRepository _profileRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly List<MemoryItem> _memories = new();

    public SimpleMemoryProvider(IProfileRepository profileRepository, IMemoryRepository memoryRepository, IPerformanceMetrics performanceMetrics)
    {
        _profileRepository = profileRepository;
        _memoryRepository = memoryRepository;
        _performanceMetrics = performanceMetrics;
    }

    public async Task Initialize(Guid characterId, ChatSessionData chat, CancellationToken cancellationToken)
    {
        var books = await _memoryRepository.GetScopeMemoryBooksAsync(characterId, cancellationToken);
        var profile = await _profileRepository.GetProfileAsync(cancellationToken) ?? throw new NullReferenceException("Profile not set");
        var culture = CultureInfo.GetCultureInfoByIetfLanguageTag(chat.Character.Culture);
        var processor = new ChatTextProcessor(profile, chat.Character.Name);
        foreach (var book in books)
        {
            _memories.AddRange(book.Items.Select(x => new MemoryItem
            {
                Id = x.Id,
                Keywords = x.Keywords,
                Text = processor.ProcessText(x.Text, culture),
                Weight = x.Weight,
            }));
        }
    }
    
    public void QueryMemoryFast(ChatSessionData chat)
    {
        var perf = _performanceMetrics.Start("SimpleMemoryProvider");
        
        // Create words index
        #warning Improve perf, e.g. by keeping a set per message. Note that if messages are updated, we should clear the words.
        #warning Memory relevance should be higher for more recent messages
        #warning We can stop as soon as we have enough tokens instead of always doing them all
        foreach (var msg in chat.GetMessages())
        {
            if (string.IsNullOrEmpty(msg.Text)) continue;
            var messageWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (Match word in WordsRegex.Matches(msg.Text))
            {
                messageWords.Add(word.Value);
            }

            foreach (var memory in _memories)
            {
                if (memory.Keywords.Any(k => messageWords.Contains(k)))
                {
                    var indexOfMemory = chat.Memories.IndexOf(memory);
                    if (indexOfMemory == 0)
                        continue;
                    if (indexOfMemory > 0)
                        chat.Memories.RemoveAt(indexOfMemory);
                    chat.Memories.Insert(0, memory);
                }
            }
        }

        perf.Done();
    }
}
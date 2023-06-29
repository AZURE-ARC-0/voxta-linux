using System.Collections.Concurrent;
using ChatMate.Abstractions.Management;
using Timer = System.Threading.Timer;

namespace ChatMate.Server.Chat
{
    public class TemporaryFileCleanupService : BackgroundService, ITemporaryFileCleanup
    {
        private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(2);
        
        private readonly ILogger<TemporaryFileCleanupService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ConcurrentDictionary<string, DateTime> _filesToDelete;
        private readonly Timer _timer;
        private bool _isRunning;

        public TemporaryFileCleanupService(ILogger<TemporaryFileCleanupService> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _filesToDelete = new ConcurrentDictionary<string, DateTime>();
            _timer = new Timer(CheckFiles);
        }

        public void MarkForDeletion(string filename)
        {
            _filesToDelete.TryAdd(filename, DateTime.Now.Add(Expiration));
            ScheduleNextCheck();
        }

        private void ScheduleNextCheck()
        {
            if (_isRunning) return;
            if (_filesToDelete.IsEmpty) return;
            var nextCheck = _filesToDelete.MinBy(f => f.Value);
            var dueTime = nextCheck.Value - DateTime.Now;
            _timer.Change(dueTime, TimeSpan.FromMilliseconds(-1));
        }

        private void CheckFiles(object? state)
        {
            _isRunning = true;

            foreach (var file in _filesToDelete.OrderBy(f => f.Value).ToList())
            {
                if (DateTime.Now < file.Value) break;
                
                try
                {
                    if (!File.Exists(file.Key)) continue;
                    File.Delete(file.Key);
                    _filesToDelete.TryRemove(file.Key, out _);
                    _logger.LogInformation("Deleted file: {File}", file.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file: {File}", file.Key);
                }
            }

            _isRunning = false;
            ScheduleNextCheck();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLifetime.ApplicationStopping.Register(() =>
            {
                _timer.Dispose();
                
                foreach (var file in _filesToDelete.Keys)
                {
                    try
                    {
                        if (!File.Exists(file)) continue;
                        File.Delete(file);
                        _logger.LogInformation("Deleted file at shutdown: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting file at shutdown: {File}", file);
                    }
                }
            });

            return Task.CompletedTask;
        }
    }
}

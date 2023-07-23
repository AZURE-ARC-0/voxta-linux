using System.Collections.Concurrent;
using ChatMate.Abstractions.Management;
using Timer = System.Threading.Timer;

namespace ChatMate.Server.BackgroundServices
{
    public class TemporaryFileCleanupService : BackgroundService, ITemporaryFileCleanup
    {
        private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(2);
        
        private readonly ILogger<TemporaryFileCleanupService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ConcurrentDictionary<string, DateTime> _filesToDelete;
        private readonly ConcurrentBag<string> _filesToDeleteOnCleanup;
        private readonly Timer _timer;
        private bool _isRunning;

        public TemporaryFileCleanupService(ILogger<TemporaryFileCleanupService> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _filesToDelete = new ConcurrentDictionary<string, DateTime>();
            _filesToDeleteOnCleanup = new ConcurrentBag<string>();
            _timer = new Timer(CheckFiles);
        }

        public void MarkForDeletion(string filename, bool reusable)
        {
            if (reusable)
            {
                _filesToDeleteOnCleanup.Add(filename);
            }
            else
            {
                _filesToDelete.TryAdd(filename, DateTime.Now.Add(Expiration));
                ScheduleNextCheck();
            }
        }

        private void ScheduleNextCheck()
        {
            if (_isRunning) return;
            if (_filesToDelete.IsEmpty) return;
            var nextCheck = _filesToDelete.MinBy(f => f.Value);
            var dueTime = nextCheck.Value - DateTime.Now;
            if (dueTime.Ticks < 0) dueTime = TimeSpan.Zero;
            _timer.Change(dueTime, TimeSpan.FromMilliseconds(-1));
        }

        private void CheckFiles(object? state)
        {
            _isRunning = true;

            foreach (var file in _filesToDelete.OrderBy(f => f.Value).ToList())
            {
                if (DateTime.Now < file.Value) break;
                DeleteFile(file.Key);
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
                    DeleteFile(file);
                }
                foreach (var file in _filesToDeleteOnCleanup)
                {
                    DeleteFile(file);
                }
            });

            return Task.CompletedTask;
        }

        private void DeleteFile(string file)
        {
            try
            {
                if (!File.Exists(file)) return;
                File.Delete(file);
                _filesToDelete.TryRemove(file, out _);
                _logger.LogInformation("Deleted file: {File}", file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {File}", file);
            }
        }
    }
}

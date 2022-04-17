namespace TesteWiProWorker
{
    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly WorkerService _workerService;
        public WindowsBackgroundService(WorkerService workerService) =>
        (_workerService) = (workerService);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _workerService.GetItemQueue();
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}
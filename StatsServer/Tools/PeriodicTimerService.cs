using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace avaness.StatsServer.Tools
{
    // Based on https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0
    public abstract class PeriodicTimerService : IHostedService, IDisposable
    {
        protected string Name;
        protected int Period = 5;

        protected readonly ILogger<PeriodicTimerService> Logger;
        private Timer timer = null!;

        protected PeriodicTimerService(ILogger<PeriodicTimerService> logger)
        {
            Logger = logger;
        }

#pragma warning disable CA1816
        public virtual void Dispose()
        {
            timer.Dispose();
        }
#pragma warning restore CA1816

        public Task StartAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation($"{Name} started with a period of {Period} seconds");

            timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(Period));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            timer.Change(Timeout.Infinite, 0);

            Logger.LogInformation($"{Name} stopped");

            return Task.CompletedTask;
        }

        protected abstract void DoWork(object state);
    }
}
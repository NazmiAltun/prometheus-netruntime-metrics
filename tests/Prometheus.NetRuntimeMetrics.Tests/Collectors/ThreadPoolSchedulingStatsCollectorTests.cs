﻿using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Prometheus.Client;
using Prometheus.Client.Collectors;
using Prometheus.NetRuntimeMetrics.Collectors;
using Prometheus.NetRuntimeMetrics.Tests.TestHelpers;
using System.Threading.Tasks;
using Xunit;

namespace Prometheus.NetRuntimeMetrics.Tests.Collectors
{
    public class ThreadPoolSchedulingStatsCollectorTests
    {
        [Fact]
        public async Task When_TasksScheduledOnThreadPool_Then_ThreadPoolStatsShouldBeCollected()
        {
            const int taskCount = 50;
            using var collector = CreateStatsCollector();
            for (var i = 0; i < taskCount; i++)
            {
                await Task.Run(() => { });
            }

            ScheduledTasksShouldBeCounted(collector, taskCount);
        }

        private void ScheduledTasksShouldBeCounted(ThreadPoolSchedulingStatsCollector collector, int taskCount)
        {
            // The reason we check  taskCount-1 here is that .net core does not emit Enqueue  
            // event for the first scheduled task ,but it only emits Dequeue event. Hence, it is not tracked.
            // IMHO , this is a minor bug in .net framework
            DelayHelper.Delay(() => collector.ScheduledCount.Value < taskCount -1 );
            collector.ScheduledCount.Value.Should().BeGreaterOrEqualTo(taskCount - 1);
            collector.ScheduleDelay.Value.Count.Should().BeGreaterOrEqualTo(taskCount - 1);
            collector.ScheduleDelay.Value.Sum.Should().BeGreaterThan(0);
        }

        private ThreadPoolSchedulingStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolSchedulingStatsCollector(
                new MetricFactory(new CollectorRegistry()),
                new MemoryCache(new MemoryCacheOptions()),
                _ => { });
        }
    }
}
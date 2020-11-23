﻿using System;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Caching.Memory;

namespace Prometheus.NetRuntimeMetrics.Utils
{
    internal class EventTimer
    {
        private readonly TimeSpan DefaultCacheDuration = TimeSpan.FromSeconds(60);

        private readonly IMemoryCache _memoryCache;
        private readonly int _startEventId;
        private readonly int _endEventId;
        private readonly Func<EventWrittenEventArgs, long> _extractEventIdFunc;
        private readonly Sampler _sampler;

        public EventTimer(
            IMemoryCache memoryCache,
            int startEventId,
            int endEventId,
            Func<EventWrittenEventArgs, long> extractEventIdFunc,
            Sampler sampler)
        {
            _startEventId = startEventId;
            _endEventId = endEventId;
            _memoryCache = memoryCache;
            _extractEventIdFunc = extractEventIdFunc;
            _sampler = sampler;
        }

        public EventTime GetEventTime(EventWrittenEventArgs e)
        {
            var key = $"{_extractEventIdFunc(e)}";

            if (e.EventId == _startEventId)
            {
                if (_sampler.ShouldSample())
                {
                    _memoryCache.Set(key, e.TimeStamp, DefaultCacheDuration);
                }

                return EventTime.Start;
            }

            if (e.EventId == _endEventId)
            {
                if (_memoryCache.TryGetValue(key, out DateTime timeStamp))
                {
                    var eventTime = new EventTime(e.TimeStamp - timeStamp);
                    _memoryCache.Remove(key);
                    return eventTime;
                }

                return EventTime.FinalWithoutDuration;
            }

            return EventTime.Unrecognized;
        }
    }
}
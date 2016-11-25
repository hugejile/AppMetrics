﻿// Copyright (c) Allan hardy. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


// Originally Written by Iulian Margarintescu https://github.com/etishor/Metrics.NET
// Ported/Refactored to .NET Standard Library by Allan Hardy

using System;
using App.Metrics.Concurrency;
using App.Metrics.Data;

namespace App.Metrics.Core
{
    public class SimpleMeter
    {
        private const int FifteenMinutes = 15;
        private const int FiveMinutes = 5;
        private const double Interval = IntervalSeconds * NanosInSecond;
        private const long IntervalSeconds = 5L;
        private const long NanosInSecond = 1000L * 1000L * 1000L;
        private const int OneMinute = 1;
        private const double SecondsPerMinute = 60.0;
        private static readonly double M1Alpha = 1 - Math.Exp(-IntervalSeconds / SecondsPerMinute / OneMinute);
        private static readonly double M5Alpha = 1 - Math.Exp(-IntervalSeconds / SecondsPerMinute / FiveMinutes);
        private static readonly double M15Alpha = 1 - Math.Exp(-IntervalSeconds / SecondsPerMinute / FifteenMinutes);


        private readonly StripedLongAdder _uncounted = new StripedLongAdder();
        private volatile bool _initialized;
        private VolatileDouble _m15Rate = new VolatileDouble(0.0);
        private VolatileDouble _m1Rate = new VolatileDouble(0.0);
        private VolatileDouble _m5Rate = new VolatileDouble(0.0);
        private AtomicLong _total = new AtomicLong(0L);

        private double FifteenMinuteRate => _m15Rate.GetValue() * NanosInSecond;

        private double FiveMinuteRate => _m5Rate.GetValue() * NanosInSecond;

        private double OneMinuteRate => _m1Rate.GetValue() * NanosInSecond;

        /// <inheritdoc />
        public virtual void Mark(long count)
        {
            _uncounted.Add(count);
        }

        /// <inheritdoc />
        public virtual void Reset()
        {
            _uncounted.Reset();
            _total.SetValue(0L);
            _m1Rate.SetValue(0.0);
            _m5Rate.SetValue(0.0);
            _m15Rate.SetValue(0.0);
        }

        /// <inheritdoc />
        public MeterValue GetValue(double elapsed)
        {
            var count = _total.GetValue() + _uncounted.GetValue();
            return new MeterValue(count, GetMeanRate(count, elapsed), OneMinuteRate, FiveMinuteRate, FifteenMinuteRate, TimeUnit.Seconds);
        }

        public void Tick()
        {
            var count = _uncounted.GetAndReset();
            Tick(count);
        }

        private static double GetMeanRate(long value, double elapsed)
        {
            if (value == 0)
            {
                return 0.0;
            }

            return value / elapsed * TimeUnit.Seconds.ToNanoseconds(1);
        }

        private void Tick(long count)
        {
            _total.Add(count);
            var instantRate = count / Interval;
            if (_initialized)
            {
                var rate = _m1Rate.GetValue();
                _m1Rate.SetValue(rate + M1Alpha * (instantRate - rate));

                rate = _m5Rate.GetValue();
                _m5Rate.SetValue(rate + M5Alpha * (instantRate - rate));

                rate = _m15Rate.GetValue();
                _m15Rate.SetValue(rate + M15Alpha * (instantRate - rate));
            }
            else
            {
                _m1Rate.SetValue(instantRate);
                _m5Rate.SetValue(instantRate);
                _m15Rate.SetValue(instantRate);
                _initialized = true;
            }
        }
    }
}
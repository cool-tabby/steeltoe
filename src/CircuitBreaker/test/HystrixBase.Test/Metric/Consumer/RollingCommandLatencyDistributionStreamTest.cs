﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCommandLatencyDistributionStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("CommandLatency");
        private RollingCommandLatencyDistributionStream stream;
        private ITestOutputHelper output;

        public RollingCommandLatencyDistributionStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            RollingCommandLatencyDistributionStream.Reset();
            HystrixCommandCompletionStream.Reset();
        }

        public override void Dispose()
        {
            base.Dispose();
            stream.Unsubscribe();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEmptyStreamProducesEmptyDistributions()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-A");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
                (distribution) =>
                {
                    output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + Thread.CurrentThread.ManagedThreadId);
                },
                (e) =>
                {
                    Assert.True(false, e.Message);
                },
                () =>
                {
                    latch.SignalEx();
                });

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        [Fact]
        public async void TestSingleBucketGetsStored()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-B");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.TIMEOUT); // latency = 600
            await cmd1.Observe();
            await cmd2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertBetween(150, 400, stream.LatestMean);
            AssertBetween(10, 100, stream.GetLatestPercentile(0.0));
            AssertBetween(300, 800, stream.GetLatestPercentile(100.0));
        }

        // The following event types should not have their latency measured:
        // THREAD_POOL_REJECTED
        // SEMAPHORE_REJECTED
        // SHORT_CIRCUITED
        // RESPONSE_FROM_CACHE
        // Newly measured (as of 1.5)
        // BAD_REQUEST
        // FAILURE
        // TIMEOUT
        [Fact]
        public async void TestSingleBucketWithMultipleEventTypes()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-C");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.TIMEOUT); // latency = 600
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 30);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST, 40);

            await cmd1.Observe();
            await cmd3.Observe();
            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd4.Observe());
            await cmd2.Observe();  // Timeout should run last

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertBetween(150, 400, stream.LatestMean); // now timeout latency of 600ms is there
            AssertBetween(10, 100, stream.GetLatestPercentile(0.0));
            AssertBetween(300, 800, stream.GetLatestPercentile(100.0));
        }

        [Fact]
        public async void TestShortCircuitedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-D");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // 3 failures is enough to trigger short-circuit.  execute those, then wait for bucket to roll
            // next command should be a short-circuit
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 3; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0));
            }

            Command shortCircuit = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                await cmd.Observe();
            }

            Time.Wait(150);

            try
            {
                await shortCircuit.Observe();
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(3, stream.Latest.GetTotalCount());
            AssertBetween(0, 75, stream.LatestMean);

            Assert.True(shortCircuit.IsResponseShortCircuited);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]               
        public async void TestThreadPoolRejectedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-E");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // 10 commands with latency should occupy the entire threadpool.  execute those, then wait for bucket to roll
            // next command should be a thread-pool rejection
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500));
            }

            Command threadPoolRejected = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                _ = cmd.ExecuteAsync();
            }

            await threadPoolRejected.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(10, stream.Latest.GetTotalCount());
            AssertBetween(500, 750, stream.LatestMean);
            Assert.True(threadPoolRejected.IsResponseThreadPoolRejected);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]       
        public async void TestSemaphoreRejectedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-F");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // 10 commands with latency should occupy all semaphores.  execute those, then wait for bucket to roll
            // next command should be a semaphore rejection
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command semaphoreRejected = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command saturator in commands)
            {
                _ = Task.Run(() => saturator.Execute());
            }

            await Task.Delay(50);

            await Task.Run(() => semaphoreRejected.Execute());
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(10, stream.Latest.GetTotalCount());
            AssertBetween(500, 750, stream.LatestMean);
            Assert.True(semaphoreRejected.IsResponseSemaphoreRejected);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestResponseFromCacheDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-G");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // should get 1 SUCCESS and 1 RESPONSE_FROM_CACHE
            List<Command> commands = Command.GetCommandsWithResponseFromCache(GroupKey, key);

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                _ = cmd.ExecuteAsync();
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, stream.Latest.GetTotalCount());
            AssertBetween(0, 75, stream.LatestMean);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]               
        public async void TestMultipleBucketsBothGetStored()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-H");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 100);

            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd5 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 70);

            await cmd1.Observe();
            await cmd2.Observe();

            Time.Wait(500);

            await cmd3.Observe();
            await cmd4.Observe();
            await cmd5.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertBetween(60, 150, stream.LatestMean);
            AssertBetween(10, 150, stream.GetLatestPercentile(0.0));
            AssertBetween(100, 150, stream.GetLatestPercentile(100.0));
        }

        [Fact]
        public async void TestMultipleBucketsBothGetStoredAndThenAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-I");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 100);

            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd5 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 70);

            await cmd1.Observe();
            await cmd2.Observe();

            Time.Wait(500);

            await cmd3.Observe();
            await cmd4.Observe();
            await cmd5.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        private void AssertBetween(int expectedLow, int expectedHigh, int value)
        {
            output.WriteLine("Low:" + expectedLow + " High:" + expectedHigh + " Value: " + value);
            Assert.InRange(value, expectedLow, expectedHigh);
        }
    }
}

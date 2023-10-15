using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if MEDIATR
using TaskType = System.Threading.Tasks.Task;
#else
using TaskType = System.Threading.Tasks.ValueTask;
#endif

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Tests
{
    public static class TestExtensions
    {
        public static Task<T> AsTask<T>(this Task<T> task) => task;

        public static void SetupRequest<TRequest, TResponse>(this TestBase source, TResponse response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(TaskType.FromResult((object)response));
        }

        public static void SetupRequest<TRequest, TResponse>(this TestBase source, Func<TRequest, CancellationToken, TResponse> response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns<TRequest, CancellationToken>((a, b) => TaskType.FromResult((object)response(a, b)));
        }

        public static void SetupRequest<TRequest, TResponse>(this TestBase source, Func<Task<TResponse>> response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async () => await response());

            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(async () => await response());
        }

        public static void SetupRequest<TRequest, TResponse>(this TestBase source, Func<TRequest, CancellationToken, Task<TResponse>> response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (TRequest r, CancellationToken c) => await response(r, c));

            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(async (TRequest r, CancellationToken c) => await response(r, c));
        }

        public static void VerifyRequest<TRequest>(this TestBase source, Times times)
        {
            source.Sender.Verify(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()), times);
        }

        public static void RunInParallell(this TestBase source, int threadCount, Func<Task> func)
        {
            var exceptions = new List<Exception>();
            Parallel.ForEachAsync(Enumerable.Range(0, threadCount), async (_, _) =>
            {
                try
                {
                    await func();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

            }).GetAwaiter().GetResult();

            if (exceptions.Any())
            {
                Assert.Fail($"One or more exceptions occured while executing in parallell:\n{string.Join('\n', exceptions.Select(e => e.ToString()))}");
            }
        }
    }

}
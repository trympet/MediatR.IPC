using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Tests
{
    public static class TestExtensions
    {
        public static void SetupRequest<TRequest, TResponse>(this TestBase source, TResponse response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((object)response));
        }

        public static void SetupRequest<TRequest, TResponse>(this TestBase source, Func<TRequest, CancellationToken, TResponse> response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns<TRequest, CancellationToken>((a, b) => Task.FromResult((object)response(a, b)));
        }

        public static void SetupRequest<TRequest, TResponse>(this TestBase source, Func<Task<TResponse>> response)
            where TRequest : IRequest<TResponse>
        {
            source.Sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .Returns(response);

            Func<Task<object>> res = async () => await response();
            source.Sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(res);
        }

        public static void VerifyRequest<TRequest>(this TestBase source, Times times)
        {
            source.Sender.Verify(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()), times);
        }

        public static void RunInParallell(this TestBase source, int threadCount, Func<Task> func)
        {
            var threads = new List<Thread>(threadCount);
            var exceptions = new List<Exception>();
            for (int i = 0; i < threadCount; i++)
            {
                var t = new Thread(() =>
                {
                    try
                    {
                        func().GetAwaiter().GetResult();
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                });

                t.Start();
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            if (exceptions.Any())
            {
                Assert.Fail("One or more exceptions occured while executing in parallell.", string.Join('\n', exceptions.Select(e => e.Message)));
            }
        }
    }

}
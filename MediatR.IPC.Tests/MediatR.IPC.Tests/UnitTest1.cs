using Moq;
using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Tests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class VoidReqeust : IRequest
    {

    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SlowReqeust : IRequest<Response>
    {

    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class Response
    {

    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RequestWithResponse : IRequest<Response>
    {

    }

    public class Tests
    {
        private Mock<ISender> sender;
        private MediatorServerPool serverPool;
        private MediatorClientPool clientPool;
        private CancellationTokenSource cts;
        private Task serverTask;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            IPCMediator.RegisterAssemblyTypes(Assembly.GetExecutingAssembly());
        }

        [SetUp]
        public void Setup()
        {
            cts = new CancellationTokenSource();
            sender = new Mock<ISender>();

            SetupRequest<VoidReqeust, Unit>(Unit.Value);
            SetupRequest<SlowReqeust, Response>(async () =>
            {
                await Task.Delay(500);
                return new Response();
            });
            SetupRequest<RequestWithResponse, Response>(new Response());

            sender.Setup(s => s.Send(It.IsAny<IRequest>(), It.Is<CancellationToken>(t => t.IsCancellationRequested)))
                .ThrowsAsync(new TaskCanceledException());

            clientPool = new MediatorClientPool("testpool", 8);
            serverPool = new MediatorServerPool(sender.Object, "testpool", 8);
            serverTask = Task.Run(serverPool.Run, cts.Token);
        }

        private void SetupRequest<TRequest, TResponse>(TResponse response)
            where TRequest : IRequest<TResponse>
        {
            sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((object)response));
        }

        private void VerifyRequest<TRequest>(Times times)
        {
            sender.Verify(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()), times);
        }

        private void SetupRequest<TRequest, TResponse>(Func<Task<TResponse>> response)
            where TRequest : IRequest<TResponse>
        {
            sender.Setup(s => s.Send<TResponse>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .Returns(response);

            Func<Task<object>> res = async () => await response();
            sender.Setup(s => s.Send(It.Is<object>(o => o is TRequest), It.IsAny<CancellationToken>()))
                .Returns(res);
        }

        [TearDown]
        public void TearDown()
        {
            serverPool.Dispose();
            cts.Cancel();
        }

        [Test]
        public void Send_SingleVoidRequest_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
        }

        [Test]
        public void Send_MultipleVoidRequests_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidReqeust()));
        }

        [Test]
        public void Send_VoidRequestsInParallell_DoesNotThrow()
        {
            RunInParallell(20, async () =>
            {
                try
                {
                    await clientPool.Send(new VoidReqeust());
                }
                catch (Exception e)
                {
                    Assert.Fail("Exception", e);
                }
            });
        }

        [Test]
        public void Send_ResponseRequestsInParallell_DoesNotThrow()
        {
            RunInParallell(20, async () =>
            {
                try
                {
                    await clientPool.Send(new RequestWithResponse());
                }
                catch (Exception e)
                {
                    Assert.Fail("Exception", e);
                }
            });
        }

        [Test]
        public void Send_SlowRequestsInParallell_DoesNotThrow()
        {
            RunInParallell(20, () => clientPool.Send(new SlowReqeust()));

            VerifyRequest<SlowReqeust>(Times.Exactly(20));
        }

        [Test]
        public void Send_ResponseRequestsInParallell_CorrectResponse()
        {
            Parallel.For(0, 20, async i =>
            {
                var response = await clientPool.Send(new RequestWithResponse());
                Assert.IsNotNull(response);
                Assert.IsInstanceOf<Response>(response);
            });
        }

        [Test]
        public async Task Send_MultipleMediators_DoesNotThrow()
        {
            cts.Cancel();
            cts.Dispose();
            serverPool.Dispose();
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var clientPool1 = new MediatorClientPool("testpool", 8);
            var serverPool1 = new MediatorServerPool(sender.Object, "testpool", 8);
            var serverTask1 = Task.Run(serverPool.Run, cts1.Token);

            await clientPool1.Send(new VoidReqeust());
            cts1.Cancel();
            cts1.Dispose();
            serverPool1.Dispose();
            var clientPool2 = new MediatorClientPool("testpool", 8);
            using var serverPool2 = new MediatorServerPool(sender.Object, "testpool", 8);
            var serverTask2 = Task.Run(serverPool.Run, cts2.Token);
            AsyncTestDelegate task = () => clientPool2.Send(new VoidReqeust());

            Assert.DoesNotThrowAsync(task);
        }

        private void RunInParallell<T>(int threadCount, Func<Task<T>> func)
        {
            var threads = new List<Thread>(threadCount);

            for (int i = 0; i < threadCount; i++)
            {
                var t = new Thread(() => _ = func().Result);
                t.Start();
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
        private void RunInParallell(int threadCount, Func<Task> func)
        {
            var threads = new List<Thread>(threadCount);

            for (int i = 0; i < threadCount; i++)
            {
                var t = new Thread(() => func().GetAwaiter().GetResult());
                t.Start();
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void RunInParallell(int threadCount, Action action)
        {
            var threads = new List<Thread>(threadCount);

            for (int i = 0; i < threadCount; i++)
            {
                var t = new Thread(() => action());
                t.Start();
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
    }
}
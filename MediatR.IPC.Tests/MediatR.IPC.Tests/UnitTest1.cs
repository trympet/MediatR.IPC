using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Tests
{
    public class TestBase
    {
        public Mock<ISender> Sender { get; set; }
    }

    [TestFixture, TestFixtureSource(nameof(PoolSizes))]
    public class Tests : TestBase
    {
        static int[] PoolSizes = new[] { 1, 2, 8 };

        private const int ParallellCount = 20;
        private int poolSize;

        private MediatorServerPool serverPool;
        private MediatorClientPool clientPool;
        private CancellationTokenSource cts;
        private Task serverTask;

        static Tests()
        {
            IPCMediator.RegisterAssemblyTypes(Assembly.GetExecutingAssembly());
            IPCMediator.UseTransport(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? IPCTransport.NamedPipe : IPCTransport.UnixDomainSocket);
        }

        public Tests(int poolSize)
        {
            this.poolSize = poolSize;
        }

        [SetUp]
        public void Setup()
        {
            cts = new CancellationTokenSource();
            Sender = new Mock<ISender>();

            this.SetupRequest<VoidRequest, Unit>(Unit.Value);
            this.SetupRequest<RequestWithResponse, Response>((a, _) => new Response { A = a.A, B = a.B, C = a.C });
            this.SetupRequest<SlowRequest, Response>(async () =>
            {
                await Task.Delay(500);
                return new Response();
            });

            Sender.Setup(s => s.Send(It.IsAny<IRequest>(), It.Is<CancellationToken>(t => t.IsCancellationRequested)))
                .ThrowsAsync(new TaskCanceledException());

            clientPool = new MediatorClientPool("testpool", poolSize);
            serverPool = new MediatorServerPool(Sender.Object, "testpool", poolSize);
            serverTask = serverPool.Run();
        }

        [TearDown]
        public async Task TearDown()
        {
            clientPool.Dispose();
            serverPool.Dispose();
            cts.Cancel();
            await serverTask;
        }

        [Test]
        public void Send_SingleVoidRequest_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidRequest()));
        }

        [Test]
        public void Send_MultipleSyncVoidRequests_DoesNotThrow()
        {
            for (int i = 0; i < ParallellCount; i++)
            {
                Assert.DoesNotThrowAsync(() => clientPool.Send(new VoidRequest()));
            }

            this.VerifyRequest<VoidRequest>(Times.Exactly(ParallellCount));
        }

        [Test]
        public async Task Send_MultipleSyncContentRequests_ContentIsCorrect()
        {
            var request1 = new RequestWithResponse { A = 1, B = true, C = null };
            var request2 = new RequestWithResponse { A = 2, B = true, C = "asdflkjasdflkjasldflkjasdflj" };
            var request3 = new RequestWithResponse { A = 3, B = false, C = null };
            var request4 = new RequestWithResponse { A = 4, B = true, C = null };

            var response1 = await clientPool.Send(request1);
            var response2 = await clientPool.Send(request2);
            var response3 = await clientPool.Send(request3);
            var response4 = await clientPool.Send(request4);

            Assert.AreEqual(request1.A, response1.A);
            Assert.AreEqual(request1.B, response1.B);
            Assert.AreEqual(request1.C, response1.C);
            Assert.AreEqual(request2.A, response2.A);
            Assert.AreEqual(request2.B, response2.B);
            Assert.AreEqual(request2.C, response2.C);
            Assert.AreEqual(request3.A, response3.A);
            Assert.AreEqual(request3.B, response3.B);
            Assert.AreEqual(request3.C, response3.C);
            Assert.AreEqual(request4.A, response4.A);
            Assert.AreEqual(request4.B, response4.B);
            Assert.AreEqual(request4.C, response4.C);
            this.VerifyRequest<RequestWithResponse>(Times.Exactly(4));
        }

        [Test]
        public void Send_VoidRequestsInParallell_DoesNotThrow()
        {
            this.RunInParallell(ParallellCount, async () =>
            {
                try
                {
                    await clientPool.Send(new VoidRequest());
                }
                catch (Exception e)
                {
                    Assert.Fail("Exception", e);
                }
            });

            this.VerifyRequest<VoidRequest>(Times.Exactly(ParallellCount));
        }

        [Test]
        public void Send_ResponseRequestsInParallell_DoesNotThrow()
        {
            this.RunInParallell(ParallellCount, async () =>
            {
                await clientPool.Send(new RequestWithResponse());
            });

            this.VerifyRequest<RequestWithResponse>(Times.Exactly(ParallellCount));
        }

        [Test]
        public void Send_SlowRequestsInParallell_DoesNotThrow()
        {
            this.RunInParallell(ParallellCount, () => clientPool.Send(new SlowRequest()));

            this.VerifyRequest<SlowRequest>(Times.Exactly(ParallellCount));
        }

        [Test]
        public async Task Send_ResponseRequestsInParallell_CorrectResponse()
        {
            int finish = 0;
            List<Exception> exceptions = new();
            Parallel.For(0, ParallellCount, async i =>
            {
                try
                {
                    var response = await clientPool.Send(new RequestWithResponse());
                    Assert.IsNotNull(response);
                    Assert.IsInstanceOf<Response>(response);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
                finish++;
            });

            while(finish < ParallellCount)
            {
                await Task.Delay(5);
            }

            Assert.IsEmpty(exceptions);
        }

        [Test]
        public async Task Send_MultipleMediators_DoesNotThrow()
        {
            clientPool.Dispose();
            serverPool.Dispose();
            cts.Cancel();
            cts.Dispose();
            await serverTask;
            cts = new CancellationTokenSource();
            clientPool = new MediatorClientPool("testpool", poolSize);
            serverPool = new MediatorServerPool(Sender.Object, "testpool", poolSize);
            serverTask = Task.Run(serverPool.Run, cts.Token);

            await clientPool.Send(new VoidRequest());
            serverPool.Dispose();
            clientPool.Dispose();
            cts.Cancel();
            cts.Dispose();
            await serverTask;
            cts = new CancellationTokenSource();
            clientPool = new MediatorClientPool("testpool", poolSize);
            serverPool = new MediatorServerPool(Sender.Object, "testpool", poolSize);
            serverTask = Task.Run(serverPool.Run, cts.Token);
            AsyncTestDelegate task = () => clientPool.Send(new VoidRequest());

            Assert.DoesNotThrowAsync(task);
        }
    }
}
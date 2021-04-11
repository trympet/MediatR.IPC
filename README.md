# MediatR.IPC
A simple IPC proxy for MediatR requests.

## Usage
All IPC requests need to be registered. This can be done via assembly scanning or explicit registration.
Since IPCs have different app domains, the registration will need to be done on the client and server.
I recommend using a shared assembly which does this for you.

#### Process 1
```csharp
public static async Task Main(string[] args)
{
    IPCMediator.UseTransport(IPCTransport.NamedPipe);
    IPCMediator.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
               .WithAttribute<IPCRequestAttribute>()
               .Where(...);
    IPCMediator.RegisterType<MyFancyQuery>();
        
    ISender sender = MyContainer.Resolve<ISender>();
    var server = new MediatorServerPool(sender, poolName: "MyRequestPool", poolSize: 8);
    await server.Run();
}
```

#### Process 2
```csharp
public static async Task Main(string[] args)
{
    IPCMediator.UseTransport(IPCTransport.NamedPipe);
    ...
    IPCMediator.RegisterType<MyFancyQuery>();
        
    ISender client = new MediatorClientPool(poolName: "MyRequestPool", poolSize: 8);
    var result = await client.Send<MyDto>(new MyFancyQuery());
    Console.WriteLine(result.SomeProperty);
}
```

## Transports
Currently, there are two supported transport types: Named Pipes and Unix Domain Sockets.
However, this can easily be expanded upon! Simply create an implementation of `IStreamStratergy`.

## Notifications
Notifications are a work in progress. The API is not completed and subject to change.

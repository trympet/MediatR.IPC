# MediatR.IPC
A simple IPC proxy for MediatR requests.

## How it works
MediatR.IPC provides two public interfaces: a client and a server. The client interface implements `ISender`. The server takes a dependency on `ISender`, usually resolved via a DI container. Messages from the client to the server are serialized with [protobuf-net](https://github.com/protobuf-net/protobuf-net). The IPC transport can use Named Pipes or Unix Domain Sockets. 

## Usage
All IPC requests need to be registered. This can be done via assembly scanning or explicit registration.
Since IPCs have different app domains, the registration will need to be done on the client and server.
I recommend using a shared assembly which does this for you.

#### Process 1
```csharp
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record MyFancyCommand : IRequest<bool>
{
    public string Message { get; init; } = string.Empty;
}

public static async Task Main(string[] args)
{
    IPCMediator.UseTransport(IPCTransport.NamedPipe);
    IPCMediator.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
               .WithAttribute<IPCRequestAttribute>()
               .Where(...);
    IPCMediator.RegisterType<MyFancyCommand>();
        
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
    IPCMediator.RegisterType<MyFancyCommand>();
        
    ISender client = new MediatorClientPool(poolName: "MyRequestPool", poolSize: 8);
    bool result = await client.Send<MyDto>(new MyFancyCommand { Message = "Hello!" });
    Console.WriteLine(result);
}
```

## Transports
Currently, there are two supported transport types: Named Pipes and Unix Domain Sockets.
However, this can easily be expanded upon! Simply create an implementation of `IStreamStratergy`.

## Notifications
Notifications are a work in progress. The API is not completed and subject to change.

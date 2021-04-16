# MediatR.IPC
A simple IPC proxy for MediatR requests.

## How it works
MediatR.IPC provides two public interfaces: a client and a server. The client interface implements `ISender`. The server takes a dependency on `ISender`, usually resolved via a DI container. Messages from the client to the server are serialized with [protobuf-net](https://github.com/protobuf-net/protobuf-net). The IPC transport can use Named Pipes or Unix Domain Sockets. 

## Usage
All IPC requests need to be registered. This can be done via assembly scanning or explicit registration.
Since IPCs have different app domains, the registration will need to be done on the client and server.
I recommend using a shared assembly which does this for you.

#### Process 1 (Backend)
```csharp
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record MyFancyCommand : IRequest<bool>
{
    public string Message { get; init; } = string.Empty;
}

public static async Task Main(string[] args)
{
    // Register the IPC requests on startup
    IPCMediator.UseTransport(IPCTransport.NamedPipe);
    IPCMediator.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
               .WithAttribute<IPCRequestAttribute>()
               .Where(...);
    IPCMediator.RegisterType<MyFancyCommand>();
        
    // Resolve the ISender, so the server can proxy incoming requests.
    ISender sender = MyContainer.Resolve<ISender>();
    
    // Run the server until the application is closed.
    var server = new MediatorServerPool(sender, poolName: "MyRequestPool", poolSize: 8);
    await server.Run();
}
```

#### Process 2 (Frontend)
```csharp
public static async Task Main(string[] args)
{
    // Register the requests, just like in Process 1
    IPCMediator.UseTransport(IPCTransport.NamedPipe);
    ...
    IPCMediator.RegisterType<MyFancyCommand>();
        
    // Create an ISender, which can be consumed by the application.
    ISender ipcSender = new MediatorClientPool(poolName: "MyRequestPool", poolSize: 8);
    
    // All requests are sent via ipcSender are sent to and handled by Process 1,
    // and the response is sent back to Process 2.
    bool result = await ipcSender.Send<MyDto>(new MyFancyCommand { Message = "Hello!" });
    Console.WriteLine(result);
}
```

## Registration
All requests sent to the client need to be registered on the client and the server. This allows for quick resolution of types, and little overhead during runtime.

## Transports
There are two supported transport types: Named Pipes and Unix Domain Sockets. Transport is specified with
```csharp
IPCMediator.UseTransport(IStreamStratergy);
```
You could also implement your own transport; a TCP transport for instance.


However, this can easily be expanded upon! Simply create an implementation of `IStreamStratergy`.

## Notifications
Notifications are a work in progress. `NotificationHandler`s need to be registered in the DI container. IPC notifications would also need to be routed to the `MediatorServer`. Pull requests are most welcome!

## Exceptions
Currently, exceptions thrown by request handlers are not serialized, and no type information is preserved. Exceptions can be huge, and there is no garantue that the client process has a reference to the `Exception`-type thrown. Ideas and suggestions are welcome!

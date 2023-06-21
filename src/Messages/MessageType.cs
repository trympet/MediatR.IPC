namespace
#if MEDIATR
MediatR.IPC.Messages
#else
Mediator.IPC.Messages
#endif
{
    internal enum MessageType : byte
    {
        Request = 0,
        Response = 1,
    }
}

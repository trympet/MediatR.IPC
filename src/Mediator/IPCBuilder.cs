using System;

namespace MediatR
{
    public class IPCBuilderContext<TContext>
    {
        internal IPCBuilderContext(TContext context)
        {
            Value = context;
        }

        internal TContext Value { get; set; }
    }

    public class IPCBuilder<TRegistration, TContext>
    {
        internal IPCBuilder(TContext context)
        {
            BuilderContext = new IPCBuilderContext<TContext>(context);
        }

        internal TContext Context
        {
            get => BuilderContext.Value;
            set => BuilderContext.Value = value;
        }

        internal IPCBuilderContext<TContext> BuilderContext { get; }

        internal IPCBuilder<TNewRegistration, TContext> Update<TNewRegistration>(TContext context)
        {
            Context = context;
            return new IPCBuilder<TNewRegistration, TContext>(context);
        }
    }
}

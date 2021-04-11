using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MediatR
{
    public static class IPCBuilderExtensions
    {
        public static IPCBuilder<Type, IEnumerable<Type>> WithAttribute<TAttribute>(this IPCBuilder<Assembly, IEnumerable<Type>> builder)
        {
            var attributeType = typeof(TAttribute);
            var types = builder.Context;
            var newContext = types.Where(t => t
                .CustomAttributes.Any(a => a.AttributeType == attributeType)
            );

            return builder.Update<Type>(newContext);
        }

        public static void Where<TContext>(this IPCBuilder<Assembly, IEnumerable<Type>> builder, Func<Type, bool> predicate)
        {
            var types = builder.Context;
            var newContext = types.Where(t => predicate(t));
            builder.Update<Type>(newContext);
        }
    }
}

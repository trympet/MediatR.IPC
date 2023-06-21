using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace
#if MEDIATR
MediatR
#else
Mediator
#endif
{
    public static class IPCBuilderExtensions
    {
        /// <summary>
        /// Registers types which implement the specified attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to filter for.</typeparam>
        /// <param name="builder">Builder</param>
        /// <returns>A builder.</returns>
        public static IPCBuilder<Type, IEnumerable<Type>> WithAttribute<TAttribute>(this IPCBuilder<Assembly, IEnumerable<Type>> builder)
        {
            var attributeType = typeof(TAttribute);
            var types = builder.Context;
            var newContext = types.Where(t => t
                .CustomAttributes.Any(a => a.AttributeType == attributeType)
            );

            return builder.Update<Type>(newContext);
        }


        /// <summary>
        /// Filters types by a given predicate.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder">Builder.</param>
        /// <param name="predicate">The predicate for filtering types.</param>
        public static void Where<TContext>(this IPCBuilder<Assembly, IEnumerable<Type>> builder, Func<Type, bool> predicate)
        {
            var types = builder.Context;
            var newContext = types.Where(t => predicate(t));
            builder.Update<Type>(newContext);
        }
    }
}

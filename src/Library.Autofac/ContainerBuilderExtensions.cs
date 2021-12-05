using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.OpenGenerics;

namespace Library.Autofac
{
    public static class ContainerBuilderExtensions
    {
        // TODO: This may not be needed
        public static IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> Register<T>(this ContainerBuilder builder, Func<IComponentContext, IDictionary<object, object>, T> @delegate) where T : notnull
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (@delegate == null) throw new ArgumentNullException(nameof(@delegate));

            T DictionaryParameterDelegate(IComponentContext context, IEnumerable<Parameter> parameters)
            {
                var parameterDictionary = parameters
                    .Select(p =>
                    {
                        if (p is TypedParameter typed) return new KeyValuePair<object, object>(typed.Type, typed.Value);
                        if (p is PositionalParameter positional) return new KeyValuePair<object, object>(positional.Position, positional.Value);
                        if (p is NamedParameter named) return new KeyValuePair<object, object>(named.Name, named.Value);

                        throw new ArgumentOutOfRangeException(nameof(parameters), "Argument not a valid parameter.");
                    })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                return @delegate(context, parameterDictionary);
            }

            var rb = RegistrationBuilder.ForDelegate(DictionaryParameterDelegate);
            rb.RegistrationData.DeferredCallback = builder.RegisterCallback(cr => RegistrationBuilder.RegisterSingleComponent(cr, rb));
            return rb;
        }

        // TODO: This may not be needed
        //    public static IRegistrationBuilder<object, OpenGenericDelegateActivatorData, DynamicRegistrationStyle> RegisterGeneric(this ContainerBuilder builder, Func<IComponentContext, Type[], IDictionary<object, object>, object> factory)
        //    {
        //        return builder.RegisterGeneric((IComponentContext c, Type[] t, IEnumerable<Parameter> typedParameters) =>
        //        {
        //            var parameterDictionary = typedParameters
        //                .Select(p =>
        //                {
        //                    if (p is TypedParameter typed) return new KeyValuePair<object, object>(typed.Type, typed.Value);
        //                    if (p is PositionalParameter positional) return new KeyValuePair<object, object>(positional.Position, positional.Value);
        //                    if (p is NamedParameter named) return new KeyValuePair<object, object>(named.Name, named.Value);

        //                    throw new ArgumentOutOfRangeException(nameof(factory), "Argument not a valid parameter.");
        //                })
        //                .ToDictionary(pair => pair.Key, pair => pair.Value);

        //            return factory(c, t, parameterDictionary);
        //        });
        //    }

        //    public static TService Resolve<TService>(this IComponentContext context, params (object, object)[] parameters)
        //    {
        //        var typedParameters = 
        //            parameters.Select(p =>
        //            {
        //                var (key, value) = p;

        //                if (key is Type type) return (Parameter) new TypedParameter(type, value);
        //                if (key is int position) return new PositionalParameter(position, value);
        //                if (key is string name) return new NamedParameter(name, value);

        //                throw new ArgumentOutOfRangeException(nameof(parameters), "Tuple argument must be a Type, Int32 or String.");
        //            });

        //        return context.Resolve<TService>(typedParameters);
        //    }
        }
    }

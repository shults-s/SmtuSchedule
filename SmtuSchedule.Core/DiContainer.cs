using System;
using System.Reflection;
using System.Collections.Generic;

namespace SmtuSchedule.Core
{
    public static class DiContainer
    {
        private static readonly IDictionary<Type, Object> Instances = new Dictionary<Type, Object>();
        private static readonly IDictionary<Type, Type> Types = new Dictionary<Type, Type>();

        private static readonly Object ReadWriteLock = new Object();

        public static T Resolve<T>() => (T)Resolve(typeof(T));

        public static Object Resolve(Type contract)
        {
            if (contract == null)
            {
                throw new ArgumentNullException(nameof(contract));
            }

            lock (ReadWriteLock)
            {
                if (!Instances.ContainsKey(contract) && !Types.ContainsKey(contract))
                {
                    throw new InvalidOperationException($"Type '{contract.FullName}' is not registered.");
                }

                if (Instances.ContainsKey(contract))
                {
                    return Instances[contract];
                }

                Type type = Types[contract];

                ConstructorInfo constructor = type.GetConstructors()[0];

                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    return Activator.CreateInstance(type);
                }

                Object[] arguments = new Object[parameters.Length];

                for (Int32 i = 0; i < parameters.Length; i++)
                {
                    arguments[i] = Resolve(parameters[i].ParameterType);
                }

                return constructor.Invoke(arguments);
            }
        }

        public static void Register<TContract, TImplementation>()
        {
            lock (ReadWriteLock)
            {
                Type type = typeof(TContract);
                if (Types.ContainsKey(type))
                {
                    throw new InvalidOperationException($"Type '{type.FullName}' is already registered.");
                }

                Types[typeof(TContract)] = typeof(TImplementation);
            }
        }

        public static void Register<TContract>(TContract instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Register<TContract, TContract>(instance);
        }

        public static void Register<TContract, TImplementation>(TImplementation instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            lock (ReadWriteLock)
            {
                Type type = instance.GetType();
                if (Instances.ContainsKey(type))
                {
                    if (Instances[type].Equals(instance))
                    {
                        return ;
                    }

                    String name = type.FullName;
                    throw new InvalidOperationException($"Instance of type '{name}' is already registered.");
                }

                Instances[typeof(TContract)] = instance;
            }
        }
    }
}
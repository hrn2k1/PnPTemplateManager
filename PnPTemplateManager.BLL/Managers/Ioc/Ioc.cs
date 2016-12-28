using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ninject;
using Ninject.Syntax;
using Ninject.Web.Common;
using Ninject.Activation;

namespace PnPTemplateManager.Managers
{
    public class Ioc
    {
        public static IKernel kernel = new StandardKernel();
        private static readonly List<Type> types = new List<Type>();

        const string ControllerScope = "ControllerScope";

        static Ioc()
        {

            //kernel.Bind<IMyComponent>().To<MyComponent>().InNamedScope(ControllerScope);
        }

        public static IEnumerable<Type> GetRegistrationTypes()
        {
            return types;
        }

        public static void RegisterType(Type interfaceType, Type implementationType)
        {
            if (types.Contains(interfaceType))
                RemoveExistingBinding(interfaceType);
            else
                types.Add(interfaceType);

            kernel.Bind(interfaceType).To(implementationType);
        }

        public static void RegisterType<T>()
        {
            if (types.Contains(typeof(T)))
                RemoveExistingBinding<T>();
            else
                types.Add(typeof(T));

            kernel.Bind<T>().ToSelf();
        }

        public static void RegisterTypeInRequestScope<T>()
        {
            if (types.Contains(typeof(T)))
                RemoveExistingBinding<T>();
            else
                types.Add(typeof(T));

            // Dfs: I have found that in request scope not always works. This is the manual way.
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
                kernel.Bind<T>().ToSelf().InScope(x => System.Web.HttpContext.Current.Request);
            else
                kernel.Bind<T>().ToSelf().InThreadScope();
        }

        public static void RegisterType<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            kernel.Bind<TInterface>().To<TImplementation>();
        }

        public static void RegisterType<TInterface, TImplementation>(string named) where TImplementation : TInterface
        {
            kernel.Bind<TInterface>().To<TImplementation>().Named(named);
        }

        public static IBindingWhenInNamedWithOrOnSyntax<TImplementation> RegisterTypeKeepExistingBindings<TInterface, TImplementation>() where TImplementation : TInterface
        {
            return kernel.Bind<TInterface>().To<TImplementation>();
        }

        public static void RegisterTypeWhenInjectedInto<TInterface, TDestination>(Func<IContext, TInterface> func)
        {
            kernel.Bind<TInterface>().ToMethod(func).WhenInjectedInto<TDestination>().InRequestScope();
        }

        public static void RegisterTypeToExistingBindingWhenAnyAnchestorIsOfType<TInterface, TImplementation>(Type ancestorType) where TImplementation : TInterface
        {
            kernel.Bind<TInterface>().ToMethod(x => x.Kernel.Get<TImplementation>()).WhenAnyAncestorMatches(x => MatchType(x, ancestorType));
        }

        private static bool AncestorMatch(IRequest x, Type ancestorType)
        {
            var request = x;
            while (request != null)
            {
                if (request.Service == ancestorType)
                    return true;

                request = request.ParentRequest;
            }
            return false;
        }

        private static bool MatchType(IContext context, Type ancestorType)
        {
            var request = context.Request;
            if (request.Service == ancestorType)
            {
                return true;
            }
            return false;
        }

        public static void RegisterType<TInterface, TImplementation, TProvider>() where TImplementation : TInterface where TProvider : IProvider
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            kernel.Bind<TInterface, TImplementation>().ToProvider<TProvider>();
        }

        public static void RegisterTypeInRequestScope<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            //kernel.Bind<TInterface>().To<TImplementation>().InScope(x => System.Web.HttpContext.Current.Request);
            kernel.Bind<TInterface>().To<TImplementation>().InRequestScope();
        }
        public static void RegisterTypeInThreadScope<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            kernel.Bind<TInterface>().To<TImplementation>().InThreadScope();
        }

        public static void RegisterType<TInterface>(Func<TInterface> factoryFunc)
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            kernel.Bind<TInterface>().ToMethod(c => factoryFunc());
        }

        public static void RegisterTypeInRequestScope<TInterface>(Func<TInterface> factoryFunc)
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            kernel.Bind<TInterface>().ToMethod(c => factoryFunc()).InRequestScope();
        }

        public static void RegisterTypeWithArgs<TInterface, TImplementation>(Dictionary<string, object> args)
            where TImplementation : TInterface
        {
            if (types.Contains(typeof(TInterface)))
                RemoveExistingBinding<TInterface>();
            else
                types.Add(typeof(TInterface));

            IBindingWhenInNamedWithOrOnSyntax<TImplementation> bindings =
                kernel.Bind<TInterface>().To<TImplementation>();
            foreach (var arg in args)
            {
                bindings.WithConstructorArgument(arg.Key, arg.Value);
            }
        }


        public static Lazy<TInterface> ResolveLazy<TInterface>()
        {
            TInterface result = default(TInterface);

            return new Lazy<TInterface>(() =>
            {
                try
                {
                    result = kernel.Get<TInterface>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("could not resolve: " + typeof(TInterface).Name);
                }

                return result;
            });
        }

        public static IEnumerable<TInterface> ResolveAll<TInterface>()
        {
            var result = default(IEnumerable<TInterface>);

            try
            {
                result = kernel.GetAll<TInterface>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("could not resolve: " + typeof(TInterface).Name);
            }

            return result;
        }

        public static TInterface Resolve<TInterface>()
        {
            TInterface result = default(TInterface);

            try
            {
                result = kernel.Get<TInterface>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("could not resolve: " + typeof(TInterface).Name);
                //System.IO.File.AppendAllText("c:\\IocError.txt",DateTime.Now + " " + nameof(TInterface) + " " + ex.Message);
            }

            return result;
        }

        public static TInterface Resolve<TInterface>(string named)
        {
            TInterface result = default(TInterface);

            try
            {
                result = kernel.Get<TInterface>(named);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"could not resolve: {typeof(TInterface).Name} with from name {named}");
            }

            return result;
        }

        [DebuggerStepThrough]
        public static object Resolve(Type serviceType)
        {
            object result = null;

            try
            {
                result = kernel.Get(serviceType);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("More than one matching bindings are available"))
                    Debug.Write("More than one binding for " + serviceType.Name);
                else
                    Debug.WriteLine("could not resolve: " + serviceType.Name, "(" + ex.Message + ")");
            }

            return result;
        }

        private static void RemoveExistingBinding(Type serviceType)
        {
            //if (kernel.TryGet(serviceType) != null)
            kernel.GetBindings(serviceType)
                .Where(binding => !binding.IsConditional)
                .ToList()
                .ForEach(b => kernel.RemoveBinding(b));
        }

        private static void RemoveExistingBinding<TInterface>()
        {
            RemoveExistingBinding(typeof(TInterface));
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace Owin.WebSocket
{
    public class GlobalContext
    {
        /// <summary>
        /// IoC container used for creating hubs
        /// </summary>
        public static IServiceLocator DependencyResolver { get; set; }


        static GlobalContext()
        {
            DependencyResolver = new DefaultDependencyResolver();
        }
    }

    internal class DefaultDependencyResolver: IServiceLocator
    {
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public object GetInstance(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }

        public object GetInstance(Type serviceType, string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public TService GetInstance<TService>()
        {
            return Activator.CreateInstance<TService>();
        }

        public TService GetInstance<TService>(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorage.Net.Internals
{
    internal class StorageRegistry
    {
        private Dictionary<string, Func<IServiceProvider, IStorage>> m_Factories;

        /// <summary>
        /// Initialize a new <see cref="StorageRegistry"/> instance.
        /// </summary>
        public StorageRegistry()
        {
            m_Factories = new Dictionary<string, Func<IServiceProvider, IStorage>>();
        }

        /// <summary>
        /// Add a new Storage Factory.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public StorageRegistry With(string Identifier, Func<IStorage> Factory)
        {
            m_Factories[Identifier] = _ => Factory.Invoke();
            return this;
        }

        /// <summary>
        /// Add a new Storage Factory.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public StorageRegistry With(string Identifier, Func<IServiceProvider, IStorage> Factory)
        {
            m_Factories[Identifier] = Factory;
            return this;
        }

        /// <summary>
        /// Try to get factory functor by identifier.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="OutFactory"></param>
        /// <returns></returns>
        public bool TryGet(string Identifier, out Func<IServiceProvider, IStorage> OutFactory)
        {
            return m_Factories.TryGetValue(Identifier, out OutFactory);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorage.Net.Internals
{
    internal class StorageRegistry
    {
        private Dictionary<string, Func<IStorage>> m_Factories;
        private Dictionary<string, IStorage> m_Instances;

        public StorageRegistry()
        {
            m_Factories = new Dictionary<string, Func<IStorage>>();
            m_Instances = new Dictionary<string, IStorage>();
        }

        /// <summary>
        /// Add a new Storage Factory.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public StorageRegistry With(string Identifier, Func<IStorage> Factory)
        {
            m_Factories[Identifier] = Factory;
            return this;
        }

        /// <summary>
        /// Get a Storage by its Identifier.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public IStorage Get(string Identifier)
        {
            lock (m_Instances)
            {
                if (m_Instances.TryGetValue(Identifier, out var Storage))
                    return Storage;

                if (m_Factories.TryGetValue(Identifier, out var Factory))
                    return m_Instances[Identifier] = Factory();

                return null;
            }
        }
    }
}

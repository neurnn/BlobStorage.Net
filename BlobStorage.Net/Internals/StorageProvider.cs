using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlobStorage.Net.Internals
{
    /// <summary>
    /// Storage Provider.
    /// </summary>
    public class StorageProvider
    {
        private readonly StorageRegistry m_Registry;
        private readonly ConcurrentDictionary<string, IStorage> m_Storages;
        private readonly IServiceProvider m_ServiceProvider;

        /// <summary>
        /// Initialize a new <see cref="StorageProvider"/> instance.
        /// </summary>
        /// <param name="Registry"></param>
        internal StorageProvider(StorageRegistry Registry, IServiceProvider Services)
        {
            m_Registry = Registry;
            m_ServiceProvider = Services;
            m_Storages = new ConcurrentDictionary<string, IStorage>();
        }

        /// <summary>
        /// 스토리지 객체를 획득합니다.
        /// 존재하지 않는 인스턴스에 접근하려고 하면,
        /// 새 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public IStorage GetStorage(string Identifier)
        {
            m_Registry.TryGet(Identifier, out var Factory);
            while (true)
            {
                if (m_Storages.TryGetValue(Identifier, out var Storage))
                    return Storage;

                // --> invoke storage factory.
                Storage = Factory.Invoke(m_ServiceProvider);

                // --> ConcurrentDictionary 인스턴스에 추가.
                if (m_Storages.TryAdd(Identifier, Storage))
                {
                    // --> 중복 생성시 파기후 다시 Get.
                    Storage.Dispose();
                }
            }
        }
    }
}

using BlobStorage.Net.Internals;
using BlobStorage.Net.Storages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorage.Net
{
    public static class Extensions
    {
        /// <summary>
        /// Get Storage Registry Instance from Service Collection.
        /// </summary>
        /// <param name="This"></param>
        /// <returns></returns>
        private static StorageRegistry GetStorageRegistry(this IServiceCollection This)
        {
            var Descriptor = This
                .Where(X => X.ServiceType == typeof(StorageRegistry))
                .FirstOrDefault();

            if (Descriptor is null)
            {
                var Instance = new StorageRegistry();
                This.AddSingleton(Instance);
                return Instance;
            }

            return Descriptor.ImplementationInstance as StorageRegistry;
        }

        /// <summary>
        /// Get Storage by Identifier.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public static IStorage GetStorage(this IServiceProvider This, string Identifier = "default")
        {
            var Registry = This.GetService<StorageRegistry>();
            if (Registry != null)
                return Registry.Get(Identifier ?? "default");

            return null;
        }

        /// <summary>
        /// Get Required Storage, And if nothing causes <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public static IStorage GetRequiredStorage(this IServiceProvider This, string Identifier = "default")
        {
            return This.GetStorage(Identifier ?? "default") 
                ?? throw new NotSupportedException($"{Identifier ?? "default"} isn't registered.");
        }

        /// <summary>
        /// Add Storage Factory.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="Identifier"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static IServiceCollection AddStorageFactory(this IServiceCollection This, string Identifier, Func<IStorage> Factory)
        {
            This.GetStorageRegistry()
                .With(Identifier ?? "default", Factory);

            return This;
        }

        /// <summary>
        /// Add Disk Storage.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="Identifier"></param>
        /// <param name="Directory"></param>
        /// <param name="HttpBase"></param>
        /// <returns></returns>
        public static IServiceCollection AddDiskStorage(this IServiceCollection This, string Identifier, DirectoryInfo Directory, Uri HttpBase = null)
        {
            This.GetStorageRegistry()
                .With(Identifier ?? "default", () => new DiskStorage(Identifier ?? "default", Directory, HttpBase));

            return This;
        }

        /// <summary>
        /// Add Memory Storage.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="Identifier"></param>
        /// <param name="SizeLimit"></param>
        /// <returns></returns>
        public static IServiceCollection AddMemoryStorage(this IServiceCollection This, string Identifier, long SizeLimit = -1)
        {
            This.GetStorageRegistry()
                .With(Identifier ?? "default", () => new MemoryStorage(Identifier ?? "default", SizeLimit));

            return This;
        }
    }
}

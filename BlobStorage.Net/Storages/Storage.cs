using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlobStorage.Net.Storages
{
    public abstract class Storage : IStorage
    {
        private static readonly Task<Uri> NO_URI_AVAILABLE = Task.FromResult<Uri>(null);
        private bool m_IsDisposed = false;

        public Storage(string Identifier)
        {
            this.Identifier = Identifier;
        }

        /// <summary>
        /// Gets this storage instance disposed or not.
        /// </summary>
        public bool IsDisposed { get { lock (this) return m_IsDisposed; } }

        /// <summary>
        /// Gets an Identifier to check equivancy.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Query the blob by its full name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public abstract Task<Blob> QueryAsync(string FullName);

        /// <summary>
        /// Make Uri asynchronously.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public virtual Task<Uri> MakeUriAsync(string FullName) => NO_URI_AVAILABLE;

        /// <summary>
        /// Make Uri String asynchronously.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public virtual async Task<string> MakeUriStringAsync(string FullName)
        {
            var Uri = await MakeUriAsync(FullName);
            if (Uri is null)
                return null;

            return Uri.ToString();
        }

        /// <summary>
        /// Returns the list of available blobs.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<Blob>> ListAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Open the blob stream to read.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<Stream> OpenReadAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Create a directory asynchronously.
        /// When this operation not supported, this always returns false.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<bool> CreateDirectoryAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Rename the blob.
        /// Warn: Some implementations of storage do hot-copy to rename the blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="NewName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<bool> RenameAsync(string FullName, string NewName, CancellationToken Token = default);

        /// <summary>
        /// Uploads a data stream as a blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="DataStream"></param>
        /// <returns></returns>
        public abstract Task<bool> WriteAsync(string FullName, Stream DataStream, CancellationToken Token = default);

        /// <summary>
        /// Delete a blob by its name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<bool> DeleteAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (m_IsDisposed)
                    return;

                m_IsDisposed = true;
            }

            Dispose(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with 
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            lock (this)
            {
                if (m_IsDisposed)
                    return;

                m_IsDisposed = true;
            }

            await DisposeAsync(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// (Implementation)
        /// </summary>
        /// <param name="Disposing"></param>
        protected virtual void Dispose(bool Disposing) => DisposeAsync(Disposing).Wait();

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// (Implementation)
        /// </summary>
        /// <param name="Disposing"></param>
        protected virtual Task DisposeAsync(bool Disposing) => Task.CompletedTask;
    }
}

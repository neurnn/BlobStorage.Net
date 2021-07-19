using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlobStorage.Net
{
    /// <summary>
    /// Storage interface.
    /// </summary>
    public interface IStorage : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets this storage instance disposed or not.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets an Identifier to check equivancy.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Query the blob by its full name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        Task<Blob> QueryAsync(string FullName);

        /// <summary>
        /// Make Uri asynchronously.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        Task<Uri> MakeUriAsync(string FullName);

        /// <summary>
        /// Returns the list of available blobs.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IEnumerable<Blob>> ListAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Open the blob stream to read.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Stream> OpenReadAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Create a directory asynchronously.
        /// When this operation not supported, this always returns false.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> CreateDirectoryAsync(string FullName, CancellationToken Token = default);

        /// <summary>
        /// Rename the blob.
        /// Warn: Some implementations of storage do hot-copy to rename the blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="NewName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> RenameAsync(string FullName, string NewName, CancellationToken Token = default);

        /// <summary>
        /// Uploads a data stream as a blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="DataStream"></param>
        /// <returns></returns>
        Task<bool> WriteAsync(string FullName, Stream DataStream, CancellationToken Token = default);

        /// <summary>
        /// Delete a blob by its name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(string FullName, CancellationToken Token = default);
    }
}

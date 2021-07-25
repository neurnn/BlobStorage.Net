using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlobStorage.Net
{
    /// <summary>
    /// Blob descriptor.
    /// </summary>
    public class Blob
    {
        private static readonly Blob[] EMPTY_BLOBS = new Blob[0];

        /// <summary>
        /// Initialize a new Blob instance.
        /// </summary>
        /// <param name="Storage"></param>
        /// <param name="FullName"></param>
        /// <param name="Kind"></param>
        public Blob(IStorage Storage, string FullName, BlobKind Kind)
        {
            this.Storage = Storage;
            this.Kind = Kind;
            this.FullName = FullName;
        }

        /// <summary>
        /// Checks if two blobs point same blob or not.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Blob _Blob)
                return $"{this}" == $"{_Blob}";

            return base.Equals(obj);
        }

        /// <summary>
        /// Make a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Storage.Identifier))
                return $"default:{FullName}";

            return $"{Storage.Identifier}:{FullName}";
        }

        /// <summary>
        /// Get Hash Code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => ToString().GetHashCode();

        /// <summary>
        /// Gets Storage instance which is source of this blob.
        /// </summary>
        public IStorage Storage { get; }

        /// <summary>
        /// Gets Kind of this blob.
        /// </summary>
        public BlobKind Kind { get; }

        /// <summary>
        /// Checks if this blob kind is file.
        /// </summary>
        public bool IsFile => Kind == BlobKind.File;

        /// <summary>
        /// Checks if this blob kind is directory.
        /// </summary>
        public bool IsDirectory => Kind == BlobKind.Directory;

        /// <summary>
        /// Full Name of this blob.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Creation time of this blob.
        /// If the underlying storage not supports, this value always DateTime.MinValue.
        /// </summary>
        public virtual DateTime CreationTime { get; } = DateTime.MinValue;

        /// <summary>
        /// Last Modification time of this blob.
        /// If the underlying storage not supports, this value always DateTime.MaxValue.
        /// </summary>
        public virtual DateTime LastModifiedTime { get; } = DateTime.MaxValue;

        /// <summary>
        /// Make Uri asynchronously.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Uri> MakeUriAsync() => await Storage.MakeUriAsync(FullName);

        /// <summary>
        /// Make Uri String asynchronously.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<string> MakeUriStringAsync() => await Storage.MakeUriStringAsync(FullName);

        /// <summary>
        /// Returns the list of available blobs.
        /// When this blob isn't a directory, returns empty enumerable.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Blob>> ListAsync(CancellationToken Token = default)
        {
            if (!IsDirectory)
                return EMPTY_BLOBS;

            return await Storage.ListAsync(FullName, Token);
        }

        /// <summary>
        /// Open the blob stream to read.
        /// When this blob isn't a file, returns null.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Stream> OpenReadAsync(CancellationToken Token = default)
        {
            if (!IsFile)
                return null;

            return await Storage.OpenReadAsync(FullName, Token);
        }

        /// <summary>
        /// Rename the blob to new name.
        /// </summary>
        /// <param name="NewFullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public virtual async Task<bool> RenameAsync(string NewName, CancellationToken Token = default) 
            => await Storage.RenameAsync(FullName, NewName, Token);

        /// <summary>
        /// Replace this blob with a new data stream.
        /// </summary>
        /// <param name="DataStream"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public virtual async Task<bool> WriteAsync(Stream DataStream, CancellationToken Token = default)
            => await Storage.WriteAsync(FullName, DataStream, Token);

        /// <summary>
        /// Delete this blob asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteAsync(CancellationToken Token = default)
            => await Storage.DeleteAsync(FullName, Token);
    }
}

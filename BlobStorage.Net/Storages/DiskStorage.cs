using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlobStorage.Net.Storages
{
    /// <summary>
    /// FileSystem Storage.
    /// </summary>
    public class DiskStorage : Storage
    {
        private static readonly Task<Blob> BLOB_NOT_FOUND = Task.FromResult<Blob>(null);
        private static readonly IEnumerable<Blob> EMPTY_BLOBS = new Blob[0];
        private static readonly Task<Stream> STREAM_NOT_FOUND = Task.FromResult<Stream>(null);
        private static readonly Task<bool> BOOL_TRUE = Task.FromResult(true);
        private static readonly Task<bool> BOOL_FALSE = Task.FromResult(false);

        private DirectoryInfo m_Directory;
        private Uri m_BaseUri;

        /// <summary>
        /// Physical Blob.
        /// </summary>
        private class PhysicalBlob : Blob
        {
            public PhysicalBlob(IStorage Storage, string FullName, BlobKind Kind)
                : base(Storage, FullName, Kind)
            {
            }

            public override DateTime CreationTime => Kind != BlobKind.Directory
                ? (Storage as DiskStorage).GetCreationTime(FullName) : base.CreationTime;

            public override DateTime LastModifiedTime => Kind != BlobKind.Directory
                ? (Storage as DiskStorage).GetLastModifiedTime(FullName) : base.LastModifiedTime;
        }

        /// <summary>
        /// Initialize a new Disk Storage using Directory Information.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Directory"></param>
        public DiskStorage(string Identifier, DirectoryInfo Directory)
            : base(Identifier) => m_Directory = Directory;

        /// <summary>
        /// Initialize a new Disk Storage using Directory Information.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Directory"></param>
        public DiskStorage(string Identifier, DirectoryInfo Directory, Uri BaseUri)
            : this(Identifier, Directory) => m_BaseUri = BaseUri;

        /// <summary>
        /// Get Creation Time.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        private DateTime GetCreationTime(string FullName)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            return (new FileInfo(RealName)).CreationTime;
        }

        /// <summary>
        /// Get Last Modified Time.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        private DateTime GetLastModifiedTime(string FullName)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            return (new FileInfo(RealName)).LastWriteTime;
        }

        /// <summary>
        /// Query the blob by its full name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public override Task<Blob> QueryAsync(string FullName)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            try
            {
                var Attribute = File.GetAttributes(RealName);
                if (Attribute.HasFlag(FileAttributes.Directory))
                    return Task.FromResult<Blob>(new PhysicalBlob(this, FullName, BlobKind.Directory));

                return Task.FromResult<Blob>(new PhysicalBlob(this, FullName, BlobKind.File));
            }

            catch { }
            return BLOB_NOT_FOUND;
        }

        /// <summary>
        /// Make Uri asynchronously.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public override Task<Uri> MakeUriAsync(string FullName)
        {
            if (m_BaseUri != null)
            {
                var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
                try
                {
                    if (File.Exists(RealName) || Directory.Exists(RealName))
                        return Task.FromResult(new Uri(m_BaseUri, FullName.TrimStart('/', '.')));
                }

                catch { }
            }

            return base.MakeUriAsync(FullName);
        }

        /// <summary>
        /// Returns the list of available blobs.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<IEnumerable<Blob>> ListAsync(string FullName, CancellationToken Token = default)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            try
            {
                var Attribute = File.GetAttributes(RealName);
                if (!Attribute.HasFlag(FileAttributes.Directory))
                    return Task.FromResult(EMPTY_BLOBS);
            }

            catch { }
            return Task.FromResult(List(FullName));
        }

        /// <summary>
        /// Returns the list of available blobs.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        private IEnumerable<Blob> List(string FullName)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            var Directory = new DirectoryInfo(RealName);

            foreach (var Each in Directory.GetDirectories())
            {
                var Name = Each.FullName
                    .Substring(m_Directory.FullName.Length)
                    .Replace('\\', '/').TrimStart('/');

                yield return new PhysicalBlob(this, Name, BlobKind.Directory);
            }

            foreach (var Each in Directory.GetFiles())
            {
                var Name = Each.FullName
                    .Substring(m_Directory.FullName.Length)
                    .Replace('\\', '/').TrimStart('/');

                yield return new PhysicalBlob(this, Name, BlobKind.File);
            }
        }

        /// <summary>
        /// Open the blob stream to read.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<Stream> OpenReadAsync(string FullName, CancellationToken Token = default)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            try { return Task.FromResult<Stream>(File.OpenRead(RealName)); }

            catch { }
            return STREAM_NOT_FOUND;
        }

        /// <summary>
        /// Create a directory asynchronously.
        /// When this operation not supported, this always returns false.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<bool> CreateDirectoryAsync(string FullName, CancellationToken Token = default)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            try
            {
                if (!Directory.Exists(RealName))
                     Directory.CreateDirectory(RealName);

                return BOOL_TRUE;
            }

            catch { }
            return BOOL_FALSE;
        }

        /// <summary>
        /// Rename the blob.
        /// Warn: Some implementations of storage do hot-copy to rename the blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="NewName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<bool> RenameAsync(string FullName, string NewName, CancellationToken Token = default)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));
            string NewRealName;

            if (NewName.StartsWith('/'))
                NewRealName = Path.Combine(m_Directory.FullName, NewName.TrimStart('/', '.'));

            else
            {
                var PathName = string.Join('/', FullName.TrimStart('/', '.').Split('/').SkipLast(1));
                NewRealName = Path.Combine(m_Directory.FullName, PathName);
            }

            try
            {
                var Attribute = File.GetAttributes(RealName);
                if (Attribute.HasFlag(FileAttributes.Directory))
                {
                    Directory.Move(RealName, NewRealName);
                    return BOOL_TRUE;
                }    

                File.Move(RealName, NewRealName);
                return BOOL_TRUE;
            }

            catch { }
            return BOOL_FALSE;
        }

        /// <summary>
        /// Uploads a data stream as a blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="DataStream"></param>
        /// <returns></returns>
        public override async Task<bool> WriteAsync(string FullName, Stream DataStream, CancellationToken Token = default)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));

            try
            {
                using (var Stream = File.OpenWrite(RealName))
                    await DataStream.CopyToAsync(Stream, Token);

                return true;
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Delete a blob by its name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<bool> DeleteAsync(string FullName, CancellationToken Token = default)
        {
            var RealName = Path.Combine(m_Directory.FullName, FullName.TrimStart('/', '.'));

            try
            {
                var Attribute = File.GetAttributes(RealName);
                if (Attribute.HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(RealName, true);
                    return BOOL_TRUE;
                }

                File.Delete(RealName);
                return BOOL_TRUE;
            }

            catch { }
            return BOOL_FALSE;
        }
    }
}

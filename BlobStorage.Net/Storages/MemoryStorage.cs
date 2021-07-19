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
    /// Memory Storage.
    /// </summary>
    public class MemoryStorage : Storage
    {
        private static readonly Task<Blob> BLOB_NOT_FOUND = Task.FromResult<Blob>(null);
        private static readonly IEnumerable<Blob> EMPTY_BLOBS = new Blob[0];
        private static readonly Task<Stream> STREAM_NOT_FOUND = Task.FromResult<Stream>(null);
        private static readonly Task<bool> BOOL_TRUE = Task.FromResult(true);
        private static readonly Task<bool> BOOL_FALSE = Task.FromResult(false);

        private List<(string Path, byte[] Data)> m_Storage = new List<(string Path, byte[] Data)>();
        private long m_SizeLeft;

        /// <summary>
        /// Initialize a new Memory Storage which has Max Size limitation.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="SizeMax"></param>
        public MemoryStorage(string Identifier, long SizeMax = long.MaxValue)
            : base(Identifier) => m_SizeLeft = SizeMax;

        /// <summary>
        /// Query the blob by its full name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public override Task<Blob> QueryAsync(string FullName)
        {
            FullName = FullName.TrimStart('/', '.');

            lock (m_Storage)
            {
                var Exactly = m_Storage.Where(X => X.Path == FullName).FirstOrDefault();
                if (Exactly.Data != null)
                    return Task.FromResult(new Blob(this, FullName, BlobKind.File));

                var Entities = m_Storage.Where(X => X.Path.StartsWith($"{FullName}/"));
                if (Entities.Count() > 0)
                    return Task.FromResult(new Blob(this, FullName, BlobKind.Directory));
            }

            return BLOB_NOT_FOUND;
        }

        /// <summary>
        /// Returns the list of available blobs.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<IEnumerable<Blob>> ListAsync(string FullName, CancellationToken Token = default)
        {
            FullName = FullName.TrimStart('/', '.');

            lock (m_Storage)
            {
                var Exactly = m_Storage.Where(X => X.Path == FullName).FirstOrDefault();
                if (Exactly.Data != null)
                    return Task.FromResult(EMPTY_BLOBS);

                var Entities = m_Storage
                    .Where(X => X.Path.StartsWith($"{FullName}/"));

                var OutBlobs = new List<Blob>();
                foreach(var Each in Entities)
                {
                    var Name = Each.Path
                        .Substring(FullName.Length).TrimStart('/', '.');

                    var Index = Name.IndexOf('/');
                    if (Index >= 0)
                    {
                        var EachFullName = string.Join('/', FullName, Name.Substring(0, Index));
                        if (OutBlobs.Find(X => X.FullName == EachFullName) is null)
                            OutBlobs.Add(new Blob(this, EachFullName, BlobKind.Directory));
                    }

                    else OutBlobs.Add(new Blob(this, Each.Path, BlobKind.File));
                }

                return Task.FromResult<IEnumerable<Blob>>(OutBlobs);
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
            FullName = FullName.TrimStart('/', '.');

            lock(m_Storage)
            {
                var Entity = m_Storage.Where(X => X.Path == FullName).FirstOrDefault();
                if (Entity.Data is null)
                    return STREAM_NOT_FOUND;

                return Task.FromResult<Stream>(new MemoryStream(Entity.Data));
            }
        }

        /// <summary>
        /// Create a directory asynchronously.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public override Task<bool> CreateDirectoryAsync(string FullName, CancellationToken Token = default)
            => BOOL_TRUE;

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
            string FinalName;

            if (NewName.StartsWith('/'))
                FinalName = NewName.TrimStart('/', '.');

            else
                FinalName = string.Join('/', FullName.TrimStart('/', '.').Split('/').SkipLast(1));

            lock(m_Storage)
            {
                var Index = m_Storage.FindIndex(X => X.Path == FullName);
                if (Index >= 0)
                {
                    var Entity = m_Storage[Index];
                    m_Storage[Index] = (Path: FinalName, Data: Entity.Data);
                    return BOOL_TRUE;
                }
            }

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
            FullName = FullName.TrimStart('/', '.');
            
            try
            {
                byte[] Bytes;

                using (var Stream = new MemoryStream())
                {
                    await DataStream.CopyToAsync(Stream);
                    Bytes = Stream.ToArray();
                }

                if (m_SizeLeft >= Bytes.Length)
                {
                    lock (m_Storage)
                    {
                        var Index = m_Storage.FindIndex(X => X.Path == FullName);
                        if (Index >= 0)
                        {
                            var Entity = m_Storage[Index];
                            m_Storage[Index] = (Path: Entity.Path, Data: Bytes);
                        }

                        else m_Storage.Add((Path: FullName, Data: Bytes));
                        m_SizeLeft -= Bytes.Length;
                    }
                    return true;
                }
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
            FullName = FullName.TrimStart('/', '.');

            lock (m_Storage)
            {
                var Index = m_Storage.FindIndex(X => X.Path == FullName);
                if (Index >= 0)
                {
                    m_SizeLeft += m_Storage[Index].Data.Length;
                    m_Storage.RemoveAt(Index);
                    return BOOL_TRUE;
                }

                var Ops = 0;
                var Count = m_Storage.Count;

                while (true)
                {
                    Index = m_Storage.FindLastIndex(
                        0, Count, X => X.Path.StartsWith($"{FullName}/"));

                    if (Index >= 0)
                    {
                        m_SizeLeft += m_Storage[Index].Data.Length;
                        m_Storage.RemoveAt(Index);

                        Count = Math.Max(0, m_Storage.Count - Index);
                        Ops++;
                        continue;
                    }

                    break;
                }

                return Ops > 0 ? BOOL_TRUE : BOOL_FALSE;
            }
        }
    }
}

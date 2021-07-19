using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlobStorage.Net
{
    public static class IStorageExtensions
    {
        private static readonly byte[] EMPTY_BLOBS = new byte[0];

        /// <summary>
        /// Read all bytes from a blob.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static async Task<int> ReadAsync(this IStorage This, string FullName, byte[] Buffer, int Offset, int Length, CancellationToken Token = default)
        {
            using (var Stream = await This.OpenReadAsync(FullName, Token))
            {
                int Total = 0;

                while (Length > 0)
                {
                    int Read = await Stream.ReadAsync(Buffer, Offset, Length);
                    if (Read <= 0) 
                        break;

                    Offset += Read;
                    Length -= Read;
                }

                return Total;
            }
        }

        /// <summary>
        /// Read a blob as byte array.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="FullName"></param>
        /// <param name="MaxLength"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReadAsync(this IStorage This, string FullName, int MaxLength = int.MaxValue, CancellationToken Token = default)
        {
            using (var Stream = await This.OpenReadAsync(FullName, Token))
            {
                if (Stream is null)
                    return null;

                var Blocks = new List<(byte[] Bytes, int Size)>();
                while (MaxLength > 0)
                {
                    var Block = new byte[4096];
                    int Read = await Stream.ReadAsync(Block, Token);
                    if (Read <= 0)
                        break;

                    Blocks.Add((Block, Math.Min(Read, MaxLength)));
                    MaxLength -= Read;
                }

                if (Blocks.Count > 0) {
                    var TotalBytes = Blocks.Sum(X => X.Size);
                    var OutBytes = Blocks[0].Bytes;
                    var OutSize = Blocks[0].Size;

                    if (OutBytes.Length != TotalBytes)
                        Array.Resize(ref OutBytes, TotalBytes);

                    foreach(var Each in Blocks.Skip(1))
                    {
                        Array.Copy(Each.Bytes, 0, OutBytes, OutSize, Each.Size);
                        OutSize += Each.Size;
                    }

                    Blocks.Clear();
                    return OutBytes;
                }

                return EMPTY_BLOBS;
            }
        }

        /// <summary>
        /// Uploads bytes as a blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Buffer"></param>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<bool> WriteAsync(this IStorage This, string FullName, byte[] Buffer, int Offset, int Length, CancellationToken Token = default)
        {
            using (var Stream = new MemoryStream(Buffer, Offset, Length))
                return await This.WriteAsync(FullName, Stream, Token);
        }

        /// <summary>
        /// Uploads bytes as a blob.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="FullName"></param>
        /// <param name="Buffer"></param>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<bool> WriteAsync(this IStorage This, string FullName, byte[] Buffer, int Length, CancellationToken Token = default)
            => This.WriteAsync(FullName, Buffer, 0, Length, Token);

        /// <summary>
        /// Uploads bytes as a blob.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="FullName"></param>
        /// <param name="Buffer"></param>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<bool> WriteAsync(this IStorage This, string FullName, byte[] Buffer, CancellationToken Token = default)
            => This.WriteAsync(FullName, Buffer, 0, Buffer.Length, Token);

        /// <summary>
        /// Query the blob by its full name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static Blob Query(this IStorage This, string FullName) => This.QueryAsync(FullName).Result;

        /// <summary>
        /// Make Uri.
        /// When the underlying storage doesn't support, this always returns null.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static Uri MakeUri(this IStorage This, string FullName) => This.MakeUriAsync(FullName).Result;

        /// <summary>
        /// Returns the list of available blobs.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static IEnumerable<Blob> List(this IStorage This, string FullName)
            => This.ListAsync(FullName).Result;

        /// <summary>
        /// Open the blob stream to read.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static Stream OpenRead(this IStorage This, string FullName)
            => This.OpenReadAsync(FullName).Result;

        /// <summary>
        /// Create a directory asynchronously.
        /// When this operation not supported, this always returns false.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static bool CreateDirectory(this IStorage This, string FullName)
            => This.CreateDirectoryAsync(FullName).Result;

        /// <summary>
        /// Rename the blob.
        /// Warn: Some implementations of storage do hot-copy to rename the blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="NewName"></param>
        /// <returns></returns>
        public static bool Rename(this IStorage This, string FullName, string NewName)
            => This.RenameAsync(FullName, NewName).Result;

        /// <summary>
        /// Uploads a data stream as a blob.
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="DataStream"></param>
        /// <returns></returns>
        public static bool Write(this IStorage This, string FullName, Stream DataStream)
            => This.WriteAsync(FullName, DataStream).Result;

        /// <summary>
        /// Delete a blob by its name.
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public static bool Delete(this IStorage This, string FullName)
            => This.DeleteAsync(FullName).Result;
    }
}

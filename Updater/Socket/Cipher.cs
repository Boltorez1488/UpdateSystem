using System.IO;
using Ionic.Zlib;

namespace Updater.Socket {
    public class Cipher {
        public byte[] Encrypt(Stream stream) {
            using (MemoryStream ms = new MemoryStream()) {
                using (var zs = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestSpeed)) {
                    stream.CopyTo(zs);
                }
                return ms.ToArray();
            }
        }

        public byte[] Encrypt(byte[] data) {
            using (MemoryStream ms = new MemoryStream()) {
                using (var zs = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestSpeed)) {
                    zs.Write(data, 0, 0);
                }
                return ms.ToArray();
            }
        }

        public byte[] Encrypt(byte[] data, int offset, int count) {
            using (MemoryStream ms = new MemoryStream()) {
                using (var zs = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestSpeed)) {
                    zs.Write(data, offset, count);
                }
                return ms.ToArray();
            };
        }

        public byte[] Decrypt(MemoryStream ms, int size) {
            using (var ps = new MemoryStream(ms.GetBuffer(), (int)ms.Position, size, false)) {
                ms.Seek(size, SeekOrigin.Current);
                using (var zs = new ZlibStream(ps, CompressionMode.Decompress)) {
                    using (var rs = new MemoryStream()) {
                        zs.CopyTo(rs);
                        return rs.ToArray();
                    }
                }
            }
        }

        public byte[] Decrypt(byte[] data, int offset, int size) {
            using (var ps = new MemoryStream(data, offset, size)) {
                using (var zs = new ZlibStream(ps, CompressionMode.Decompress)) {
                    using (var rs = new MemoryStream()) {
                        zs.CopyTo(rs);
                        return rs.ToArray();
                    }
                }
            }
        }
    }
}

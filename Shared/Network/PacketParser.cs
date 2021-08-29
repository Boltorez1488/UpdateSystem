using Google.Protobuf;
using Shared.Opcodes;
using System;
using System.IO;

namespace Shared.Network {
    public static class PacketParser {
        //protected virtual byte[] EncryptBody(byte[] data, int offset, int sz) { return data; }
        //protected virtual byte[] EncryptBody(Stream stream) { return null; }
        //protected virtual byte[] DecryptBody(byte[] data, int offset, int sz) { return data; }

        public static byte[] PackPacket(IMessage packet) {
            var op = OpcodeIO.GetOpcode(packet);
            if (op == 0)
                return null;

            var dataSize = packet.CalculateSize() + 1;

            int endSize = 0;
            var sizeData = new byte[5];
            var p1 = (byte)dataSize;
            var p2 = (byte)(dataSize >> 8);
            var p3 = (byte)(dataSize >> 16);
            var p4 = (byte)(dataSize >> 24);
            if (p1 != 0) {
                sizeData[endSize++] = p1;
            }
            if (p2 != 0) {
                sizeData[endSize++] = p2;
            }
            if (p3 != 0) {
                sizeData[endSize++] = p3;
            }
            if (p4 != 0) {
                sizeData[endSize++] = p4;
            }

            var output = new byte[endSize + 1 + dataSize];
            Array.Copy(sizeData, output, endSize);
            output[endSize + 1] = op;
            using (var ms = new MemoryStream(output, endSize + 2, dataSize - 1)) {
                packet.WriteTo(ms);
            }
            return output;
        }

        //private byte[] Result(byte[] res) {
        //    var dataSize = res.Length;

        //    int endSize = 0;
        //    var sizeData = new byte[5];
        //    var p1 = (byte)dataSize;
        //    var p2 = (byte)(dataSize >> 8);
        //    var p3 = (byte)(dataSize >> 16);
        //    var p4 = (byte)(dataSize >> 24);
        //    if (p1 != 0) {
        //        sizeData[endSize++] = p1;
        //    }
        //    if (p2 != 0) {
        //        sizeData[endSize++] = p2;
        //    }
        //    if (p3 != 0) {
        //        sizeData[endSize++] = p3;
        //    }
        //    if (p4 != 0) {
        //        sizeData[endSize++] = p4;
        //    }

        //    var output = new byte[endSize + 1 + res.Length];
        //    Array.Copy(sizeData, output, endSize);
        //    Array.Copy(res, 0, output, endSize + 1, res.Length);
        //    return output;
        //}

        //public byte[] PackData(Stream stream) {
        //    return Result(EncryptBody(stream));
        //}

        //public byte[] PackData(byte[] data) {
        //    return Result(EncryptBody(data, 0, data.Length));
        //}

        //public byte[] PackData(byte[] data, int offset, int size) {
        //    return Result(EncryptBody(data, offset, size));
        //}

        public static (byte[] Data, int Offset, int Size) UnpackData(byte[] data, ref int offset, out bool isPartCut, int allow = 0) {
            return UnpackData(data, data.Length, ref offset, out isPartCut, allow);
        }

        public static (byte[] Data, int Offset, int Size) UnpackData(byte[] data, int size, ref int offset, out bool isPartCut, int allow = 0) {
            isPartCut = false;
            if (size == 0 || offset >= size)
                return (null, 0, 0);

            int i = 0;
            int sizeData = 0;
            for(int off = 0; i < 4; i++) {
                if (data[offset + i] == 0) break;
                sizeData |= data[offset + i] << off;
                off += 8;
            }

            if (i == 4 && data[offset + i] != 0) 
                throw new Exception("Packet header is broken");

            if (allow != 0 && sizeData > allow)
                throw new Exception("Packet size of data is too large");

            if (offset + i + 1 + (uint)sizeData > size) {
                isPartCut = true;
                return (null, 0, 0);
            }

            var res = (data, offset + i + 1, sizeData);
            offset += i + 1 + sizeData;
            return res;
        }
    }
}

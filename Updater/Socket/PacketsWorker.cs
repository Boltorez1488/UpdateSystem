using Shared.Opcodes;
using System;
using Shared.Network;

namespace Updater.Socket {
    public partial class PacketsWorker {
        public Session Session { get; }

        public byte[] RemainData;

        public PacketsWorker(Session session) {
            Session = session;
        }

        public void ParsePacket(byte[] data, int offset, int size) {
            if (size == 0)
                return;

            var op = data[offset];
            if (op == 0)
                return;

            var handler = OpcodeIO.GetHandler(op);
            if (!handler.HasValue)
                return;

            var packet = handler.Value.Parser.ParseFrom(data, offset + 1, size - 1);
            handler.Value.Action.Invoke(Session, new object[] { packet });
        }

        public void ReadData(byte[] data, long offset, long size) {
            byte[] bytes;
            if (size != data.Length) {
                bytes = new byte[size];
                Array.Copy(data, bytes, size);
            } else {
                bytes = data;
            }

            int off = 0;
            if (RemainData != null) {
                var over = new byte[RemainData.Length + bytes.Length];
                Array.Copy(RemainData, over, RemainData.Length);
                Array.Copy(bytes, offset, over, RemainData.Length, bytes.Length - offset);
                bytes = over;
                RemainData = null;
            } else {
                off = (int)offset;
            }

            while (true) {
                var res = PacketParser.UnpackData(bytes, ref off, out var isPartCut);
                if (res.Data == null) {
                    if (isPartCut) {
                        RemainData = new byte[bytes.Length - off];
                        Array.Copy(bytes, off, RemainData, 0, RemainData.Length);
                    }
                    break;
                }
                ParsePacket(res.Data, res.Offset, res.Size);
            }
        }
    }
}

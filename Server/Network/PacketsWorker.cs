using Shared.Opcodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using Shared.Network;

namespace Server {
    public class Buffer {
        public int Offset;
        public byte[] Data;
    }

    public partial class PacketsWorker {
        public Session Session { get; }
        public AntiDDoS Spam { get; }

        private readonly Queue<Buffer> buffers = new();
        public bool NeedReadRemainBuffer => buffers.Count > 0;

        public PacketsWorker(Session session) {
            Session = session;
            Spam = new AntiDDoS();
        }

        public void ParsePacket(byte[] data, int offset, int size) {
            if (size == 0)
                return;

            var op = data[offset];
            if (op == 0)
                return;

            Spam.AddOpcode(op);
            var handler = OpcodeIO.GetHandler(op);
            if (!handler.HasValue)
                return;

            var packet = handler.Value.Parser.ParseFrom(data, offset + 1, size - 1);
            handler.Value.Action.Invoke(Session, new object[] { packet });
        }

        public void ReadRemain() {
            lock (this) {
                uint counter = 0;
                while(buffers.Count > 0) {
                    var buff = buffers.Peek();
                    var res = PacketParser.UnpackData(buff.Data, ref buff.Offset, out var _, 32 * 1024);
                    if (res.Data == null) {
                        buffers.Dequeue();
                        break;
                    }
                    ParsePacket(res.Data, res.Offset, res.Size);
                    counter++;
                    if (counter > Config.Srv.MaxPacketsReadPerTick) {
                        break;
                    }
                }
            }
        }

        public void ReadData(byte[] data, long offset, long size) {
            lock (this) {
                if (size != data.Length) {
                    var newData = new byte[size];
                    Array.Copy(data, newData, size);
                    buffers.Enqueue(new Buffer {
                        Data = newData,
                        Offset = (int)offset
                    });
                } else {
                    buffers.Enqueue(new Buffer {
                        Data = data,
                        Offset = (int)offset
                    });
                }
                ReadRemain();
            }
        }
    }
}

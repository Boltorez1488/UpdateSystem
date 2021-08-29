using Server.Managers;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace Server {
    public partial class Session {
        public void OnAlive(ClientPackets.AliveTick packet) {
            LastAliveTick = DateTime.UtcNow;
        }

        public void OnCrcRequest(ClientPackets.CrcRequest packet) {
            var list = new ServerPackets.CrcList();
            if (packet.FileIds.Count == 0) {
                foreach (var kv in FileManager.Files) {
                    list.Items.Add(new ServerPackets.CrcList.Types.CrcItem {
                        Path = kv.Key,
                        Crc = kv.Value.CRC,
                        Size = kv.Value.Size,
                        FileId = kv.Value.Id,
                        Url = $"http://{Config.Srv.WebUrl}/{kv.Key}"
                    });
                }
            } else {
                var set = new HashSet<ulong>(packet.FileIds);
                foreach (var kv in FileManager.Files) {
                    if (!set.Contains(kv.Value.Id))
                        continue;
                    list.Items.Add(new ServerPackets.CrcList.Types.CrcItem {
                        Path = kv.Key,
                        Crc = kv.Value.CRC,
                        Size = kv.Value.Size,
                        FileId = kv.Value.Id,
                        Url = $"http://{Config.Srv.WebUrl}/{kv.Key}"
                    });
                }
            }
            Send(list);

            if (FileManager.Remover.Count != 0) {
                var r = new ServerPackets.FilesRemove();
                r.Pathes.AddRange(FileManager.Remover);
                Send(r);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Server {
    public class SpamException : Exception {
        public long Opcode { get; }
        public uint Counter { get; }

        public SpamException(string message, long op, uint counter) : base(message) {
            Opcode = op;
            Counter = counter;
        }
    }

    public class AntiDDoS {
        public struct Opcode {
            public DateTime LastTime;
            public uint Count;
        }
        public Dictionary<long, Opcode> Opcodes { get; } = new();

        public void AddOpcode(long opcode) {
            if (!Opcodes.ContainsKey(opcode)) {
                Opcodes[opcode] = new Opcode { 
                    LastTime = DateTime.UtcNow,
                    Count = 0
                };
            }

            var counter = Opcodes[opcode];
            var diff = DateTime.UtcNow - Opcodes[opcode].LastTime;
            if (diff.TotalSeconds > Config.Srv.Spam.Seconds) {
                counter.LastTime = DateTime.UtcNow;
                counter.Count = 1;
                Opcodes[opcode] = counter;
            }

            counter.Count++;
            Opcodes[opcode] = counter;
            if (counter.Count > Config.Srv.Spam.Allow) {
                throw new SpamException($"Spam detected: {opcode} = {counter.Count}", opcode, counter.Count);
            }
        }
    }

}

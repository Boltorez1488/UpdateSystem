using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shared.Opcodes {
    public struct OpHandler {
        public MessageParser Parser;
        public MethodInfo Action;
    }

    public static class OpcodeIO {
        public static Dictionary<byte, OpHandler> Handlers = new Dictionary<byte, OpHandler>();
        public static Dictionary<MessageDescriptor, byte> Senders = new Dictionary<MessageDescriptor, byte>();

        public static byte GetOpcode(IMessage msg) {
            if (Senders.ContainsKey(msg.Descriptor)) {
                return Senders[msg.Descriptor];
            }
            return 0;
        }

        public static OpHandler? GetHandler(byte opcode) {
            if (Handlers.ContainsKey(opcode)) {
                return Handlers[opcode];
            }
            return null;
        }

        // Namespace.Session, ServerPackets
        public static void Start(string senderPath, string senderNamespace) {
            var methods = Type.GetType(senderPath)
                ?.GetMethods()
                .Where(x => x.Name.StartsWith("On") && x.GetParameters().Length > 0);

            if (methods == null)
                return;

            foreach (var m in methods) {
                var p = m.GetParameters()[0];
                if (p.ParameterType.GetField("Opcode") == null)
                    continue;

                var op = (byte)p.ParameterType.GetField("Opcode").GetValue(null);
                Handlers[op] = new OpHandler {
                    Action = m,
                    Parser = (MessageParser)p.ParameterType.GetProperty("Parser")?.GetValue(null)
                };
            }

            StartSenders(senderNamespace);
        }

        private static void StartSenders(string space) {
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes()
                .Where(x => x.GetInterface("Google.Protobuf.IMessage") != null)
                .Where(x => x.FullName != null && x.IsClass && x.FullName.StartsWith(space));
            foreach(var t in types) {
                if (t.GetField("Opcode") == null)
                    continue;
                var op = (byte)t.GetField("Opcode").GetValue(null);
                var desc = (MessageDescriptor)t.GetProperty("Descriptor")?.GetValue(null);
                Senders[desc ?? throw new InvalidOperationException()] = op;
            }
        }
    }
}

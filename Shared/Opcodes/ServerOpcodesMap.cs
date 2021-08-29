namespace ServerPackets {
	public sealed partial class CrcList {
		public static readonly byte Opcode = 1;
	}
}

namespace ServerPackets {
	public sealed partial class CrcUpdate {
		public static readonly byte Opcode = 2;
	}
}

namespace ServerPackets {
	public sealed partial class DownloadPart {
		public static readonly byte Opcode = 3;
	}
}

namespace ServerPackets {
	public sealed partial class DownloadWaitNext {
		public static readonly byte Opcode = 4;
	}
}

namespace ServerPackets {
	public sealed partial class DownloadStatus {
		public static readonly byte Opcode = 5;
	}
}

namespace ServerPackets {
	public sealed partial class DownloadLockable {
		public static readonly byte Opcode = 6;
	}
}

namespace ServerPackets {
	public sealed partial class FilesRemove {
		public static readonly byte Opcode = 7;
	}
}


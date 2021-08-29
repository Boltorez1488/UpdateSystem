namespace ClientPackets {
	public sealed partial class AliveTick {
		public static readonly byte Opcode = 1;
	}
}

namespace ClientPackets {
	public sealed partial class CrcRequest {
		public static readonly byte Opcode = 2;
	}
}

namespace ClientPackets {
	public sealed partial class DownloadStart {
		public static readonly byte Opcode = 3;
	}
}

namespace ClientPackets {
	public sealed partial class DownloadNextPart {
		public static readonly byte Opcode = 4;
	}
}

namespace ClientPackets {
	public sealed partial class DownloadCancel {
		public static readonly byte Opcode = 5;
	}
}

namespace ClientPackets {
	public sealed partial class DownloadStarted {
		public static readonly byte Opcode = 6;
	}
}

namespace ClientPackets {
	public sealed partial class DownloadFinish {
		public static readonly byte Opcode = 7;
	}
}

namespace ClientPackets {
	public sealed partial class DownloadCanceled {
		public static readonly byte Opcode = 8;
	}
}


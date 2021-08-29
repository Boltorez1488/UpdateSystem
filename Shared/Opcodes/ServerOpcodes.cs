enum ServerOpcodes : byte {
	ServerPackets_CrcList = 1,
	ServerPackets_CrcUpdate = 2,
	ServerPackets_DownloadPart = 3,
	ServerPackets_DownloadWaitNext = 4,
	ServerPackets_DownloadStatus = 5,
	ServerPackets_DownloadLockable = 6,
	ServerPackets_FilesRemove = 7,
}

enum ClientOpcodes : byte {
	ClientPackets_AliveTick = 1,
	ClientPackets_CrcRequest = 2,
	ClientPackets_DownloadStart = 3,
	ClientPackets_DownloadNextPart = 4,
	ClientPackets_DownloadCancel = 5,
	ClientPackets_DownloadStarted = 6,
	ClientPackets_DownloadFinish = 7,
	ClientPackets_DownloadCanceled = 8,
}

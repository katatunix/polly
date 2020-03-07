#ifdef NODEVFEE
	#include <WinSock2.h>
	#include <Mswsock.h>
	#include "PoolHub.h"
	#include "Wallet.h"
#endif
#include <stdio.h>
#include "minhook/MinHook.h"
#include "log.h"
#include "Pipe.h"

int (*__stdio_common_vfprintf_original)(unsigned __int64 _Options, FILE* _Stream, char const* _Format, _locale_t _Locale, va_list _ArgList);
BOOL (__stdcall *WriteFileOriginal)(HANDLE hFile, LPCVOID lpBuffer, DWORD nNumberOfBytesToWrite, LPDWORD lpNumberOfBytesWritten, LPOVERLAPPED lpOverlapped);
#ifdef NODEVFEE
int (__stdcall *connectOriginal)(SOCKET s, const struct sockaddr *name, int namelen);
typedef BOOL (__stdcall *ConnectExPtr)(SOCKET s, const struct sockaddr *name, int namelen, PVOID lpSendBuffer,
									DWORD dwSendDataLength, LPDWORD lpdwBytesSent, LPOVERLAPPED lpOverlapped);
ConnectExPtr connectExOriginal;
int (__stdcall *WSAIoctlOriginal)(SOCKET s, DWORD dwIoControlCode, LPVOID lpvInBuffer, DWORD cbInBuffer, LPVOID lpvOutBuffer,
								DWORD cbOutBuffer, LPDWORD lpcbBytesReturned, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine);

int (__stdcall *sendOriginal)(SOCKET s, const char *buf, int len, int flags);
int (__stdcall *WSASendOriginal)(SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount,
								LPDWORD lpNumberOfBytesSent, DWORD dwFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine);
#endif

Pipe pipe("Polly");
#ifdef NODEVFEE
PoolHub poolHub("pools.txt");
Wallet wallet;
#endif

int __stdio_common_vfprintf_hooked(unsigned __int64 _Options, FILE* _Stream, char const* _Format, _locale_t _Locale, va_list _ArgList) {
	auto ret = (*__stdio_common_vfprintf_original)(_Options, _Stream, _Format, _Locale, _ArgList);
	pipe.write(_Options, _Format, _Locale, _ArgList, WriteFileOriginal);
	return ret;
}

BOOL __stdcall WriteFileHooked(HANDLE hFile, LPCVOID lpBuffer, DWORD nNumberOfBytesToWrite, LPDWORD lpNumberOfBytesWritten, LPOVERLAPPED lpOverlapped) {
	auto ret = (*WriteFileOriginal)(hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, lpOverlapped);
	static auto stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
	if (hFile == stdOut) {
		pipe.write(lpBuffer, *lpNumberOfBytesWritten, WriteFileOriginal);
	}
	return ret;
}

#ifdef NODEVFEE
int __stdcall connectHooked(SOCKET s, const struct sockaddr* name, int namelen) {
	auto hackedName = poolHub.process(*name);
	return (*connectOriginal)(s, &hackedName, namelen);
}

BOOL __stdcall connectExHooked(SOCKET s, const struct sockaddr *name, int namelen, PVOID lpSendBuffer,
							DWORD dwSendDataLength, LPDWORD lpdwBytesSent, LPOVERLAPPED lpOverlapped) {
	auto hackedName = poolHub.process(*name);
	return (*connectExOriginal)(s, &hackedName, namelen, lpSendBuffer, dwSendDataLength, lpdwBytesSent, lpOverlapped);
}

int __stdcall WSAIoctlHooked(SOCKET s, DWORD dwIoControlCode, LPVOID lpvInBuffer, DWORD cbInBuffer, LPVOID lpvOutBuffer, DWORD cbOutBuffer, LPDWORD lpcbBytesReturned,
							LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine) {
	auto ret = WSAIoctlOriginal(s, dwIoControlCode, lpvInBuffer, cbInBuffer, lpvOutBuffer, cbOutBuffer, lpcbBytesReturned, lpOverlapped, lpCompletionRoutine);
	if (dwIoControlCode == SIO_GET_EXTENSION_FUNCTION_POINTER) {
		GUID GUIDConnectEx = WSAID_CONNECTEX;
		if (cbInBuffer == sizeof(GUIDConnectEx)) {
			if (!memcmp(lpvInBuffer, &GUIDConnectEx, cbInBuffer)) {
				connectExOriginal = *((ConnectExPtr*)lpvOutBuffer);
				*((ConnectExPtr*)lpvOutBuffer) = connectExHooked;
			}
		}
	}
	return ret;
}

int __stdcall sendHooked(SOCKET s, const char* buf, int len, int flags) {
	auto hackedBuf = wallet.process(buf, len);
	auto ret = sendOriginal(s, hackedBuf, len, flags);
	delete hackedBuf;
	return ret;
}

int __stdcall WSASendHooked(SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesSent, DWORD dwFlags,
							LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
	auto hackedBuffers = new WSABUF[dwBufferCount];
	for (auto i = 0; i < dwBufferCount; ++i) {
		auto len = lpBuffers[i].len;
		hackedBuffers[i].buf = wallet.process(lpBuffers[i].buf, len);
		hackedBuffers[i].len = len;
	}
	auto ret = WSASendOriginal(s, hackedBuffers, dwBufferCount, lpNumberOfBytesSent, dwFlags, lpOverlapped, lpCompletionRoutine);
	for (auto i = 0; i < dwBufferCount; ++i) {
		delete hackedBuffers[i].buf;
	}
	delete [] hackedBuffers;
	return ret;
}
#endif

void start() {
	pipe.writeCurrentProcessId(&WriteFile);

	MH_Initialize();

	MH_CreateHookApi(L"ucrtbase.dll", "__stdio_common_vfprintf", __stdio_common_vfprintf_hooked, (void**)&__stdio_common_vfprintf_original);
	MH_CreateHookApi(L"kernel32.dll", "WriteFile", WriteFileHooked, (void**)&WriteFileOriginal);
#ifdef NODEVFEE
	MH_CreateHookApi(L"ws2_32.dll", "connect", connectHooked, (void**)&connectOriginal);
	MH_CreateHookApi(L"ws2_32.dll", "WSAIoctl", WSAIoctlHooked, (void**) &WSAIoctlOriginal);
	MH_CreateHookApi(L"ws2_32.dll", "send", sendHooked, (void**)&sendOriginal);
	MH_CreateHookApi(L"ws2_32.dll", "WSASend", WSASendHooked, (void**)&WSASendOriginal);
#endif

	MH_EnableHook(MH_ALL_HOOKS);
}

int DllMain(HINSTANCE instance, unsigned int reason, void* reserved) {
	switch (reason) {
		case DLL_PROCESS_ATTACH:
			start();
			break;

		case DLL_PROCESS_DETACH:
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
			break;
	}
	return true;
}

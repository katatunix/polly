#include <stdio.h>
#ifdef NODEVFEE
#include <WinSock2.h>
#endif
#include "minhook/MinHook.h"

int(*__stdio_common_vfprintf_original)(
	unsigned __int64 _Options,
	FILE* _Stream,
	char const* _Format,
	_locale_t _Locale,
	va_list _ArgList);

BOOL (WINAPI* WriteFileOriginal)(
	HANDLE hFile,
	LPCVOID lpBuffer,
	DWORD nNumberOfBytesToWrite,
	LPDWORD lpNumberOfBytesWritten,
	LPOVERLAPPED lpOverlapped);

#ifdef NODEVFEE
int(*sendOriginal)(SOCKET s, const char *buf, int len, int flags);
#endif

HANDLE pipe, stdOut;

#ifdef NODEVFEE
const int WALLET_LEN = 42;
char myWallet[WALLET_LEN + 1] = { 0 };
bool myWalletLoaded = false;
#endif

void init()
{
	pipe = CreateFile(
		L"\\\\.\\pipe\\Polly",
		GENERIC_WRITE,
		FILE_SHARE_READ | FILE_SHARE_WRITE,
		NULL,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		NULL);
	stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
	
	char buffer[16];
	sprintf(buffer, "%d\n", (int)GetCurrentProcessId());
	unsigned long written = 0;
	WriteFile(pipe, (const void*)buffer, (unsigned long)strlen(buffer), &written, NULL);
}

void writePipe(const void* s, unsigned long len)
{
	unsigned long written = 0;
	(*WriteFileOriginal)(pipe, s, len, &written, NULL);
}

void quit()
{
	CloseHandle(pipe);
}

int __stdio_common_vfprintf_hooked(
	unsigned __int64 _Options,
	FILE* _Stream,
	char const* _Format,
	_locale_t _Locale,
	va_list _ArgList)
{
	auto ret = (*__stdio_common_vfprintf_original)(_Options, _Stream, _Format, _Locale, _ArgList);
	char buffer[1024];
	int len = __stdio_common_vsprintf(_Options, buffer, 1024, _Format, _Locale, _ArgList);
	writePipe(buffer, len);
	return ret;
}

BOOL WINAPI WriteFileHooked(
	HANDLE hFile,
	LPCVOID lpBuffer,
	DWORD nNumberOfBytesToWrite,
	LPDWORD lpNumberOfBytesWritten,
	LPOVERLAPPED lpOverlapped)
{
	auto ret = (*WriteFileOriginal)(hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, lpOverlapped);
	if (hFile == stdOut)
	{
		writePipe(lpBuffer, *lpNumberOfBytesWritten);
	}
	return ret;
}

#ifdef NODEVFEE
void processWallet(const char* wallet)
{
	if (!myWalletLoaded)
	{
		memcpy(myWallet, wallet, WALLET_LEN);
		myWalletLoaded = true;
	}
	else
	{
		memcpy((void*)wallet, myWallet, WALLET_LEN);
	}
}

void handleSubmitLogin(const char* buf)
{
	auto wallet = strstr(buf, "\"params\": [\"");
	if (wallet)
	{
		wallet += 12;
		processWallet(wallet);
		printf("NoDevFee: eth_submitLogin -> %s\n", myWallet);
	}
	else
	{
		printf("NoDevFee: eth_submitLogin -> Error\n");
	}
}

void handleLogin(const char* buf)
{
	auto wallet = strstr(buf, "\"params\":[\"");
	if (wallet)
	{
		wallet += 11;
		processWallet(wallet);
		printf("NoDevFee: eth_login -> %s\n", myWallet);
	}
	else
	{
		printf("NoDevFee: eth_login -> Error\n");
	}
}

int sendHooked(SOCKET s, const char *buf, int len, int flags)
{
	if (strstr(buf, "eth_submitLogin") != 0)
	{
		handleSubmitLogin(buf);
	}
	else if (strstr(buf, "eth_login") != 0)
	{
		handleLogin(buf);
	}
	return sendOriginal(s, buf, len, flags);
}
#endif

void hook()
{
	MH_Initialize();

	MH_CreateHookApi(L"ucrtbase.dll", "__stdio_common_vfprintf", __stdio_common_vfprintf_hooked, (void**)&__stdio_common_vfprintf_original);
	MH_CreateHookApi(L"kernel32.dll", "WriteFile", WriteFileHooked, (void**)&WriteFileOriginal);
#ifdef NODEVFEE
	MH_CreateHookApi(L"ws2_32.dll", "send", sendHooked, (void**)&sendOriginal);
#endif

	MH_EnableHook(MH_ALL_HOOKS);
}

int DllMain(HINSTANCE instance, unsigned int reason, void* reserved)
{
	switch (reason)
	{
	case DLL_PROCESS_ATTACH:
		init();
		hook();
		break;

	case DLL_PROCESS_DETACH:
		quit();
		break;

	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
		break;
	}
	return true;
}

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
int(*connectOriginal)(SOCKET s, const struct sockaddr *name, int namelen);
#endif

HANDLE pipe, stdOut;

#ifdef NODEVFEE
const int WALLET_LEN = 42;
char myWallet[WALLET_LEN + 1] = { 0 };
bool myWalletLoaded = false;
struct Pool
{
	char domain[256];
	unsigned int port;
};
Pool myPool;
enum MyPoolState { NOT_LOADED, LOADED_FAIL, LOADED_OK };
MyPoolState myPoolState = NOT_LOADED;
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
		printf("NoDevFee: saved my wallet address %s\n", myWallet);
	}
	else
	{
		char buf[WALLET_LEN + 1] = { 0 };
		memcpy(buf, (void*)wallet, WALLET_LEN);
		memcpy((void*)wallet, myWallet, WALLET_LEN);
		printf("NoDevFee: modified wallet address %s -> %s\n", buf, myWallet);
	}
}

void handleSubmitLogin(const char* buf)
{
	auto wallet = strstr(buf, "\"params\": [\"");
	if (wallet)
	{
		wallet += 12;
		processWallet(wallet);
	}
}

void handleLogin(const char* buf)
{
	auto wallet = strstr(buf, "\"params\":[\"");
	if (wallet)
	{
		wallet += 11;
		processWallet(wallet);
	}
}

int sendHooked(SOCKET s, const char* buf, int len, int flags)
{
	if (strstr(buf, "eth_submitLogin"))
	{
		printf("NoDevFee: eth_submitLogin detected\n");
		handleSubmitLogin(buf);
	}
	else if (strstr(buf, "eth_login"))
	{
		printf("NoDevFee: eth_login detected\n");
		handleLogin(buf);
	}
	return sendOriginal(s, buf, len, flags);
}

int connectHooked(SOCKET s, const struct sockaddr* name, int namelen)
{
	static sockaddr tmpAddress;
	memcpy(&tmpAddress, name, namelen);

	if (myPoolState == NOT_LOADED)
	{
		auto f = fopen("pool.txt", "rt");
		myPoolState = LOADED_FAIL;
		if (f)
		{
			char buf[256];
			if (fgets(buf, sizeof(buf), f))
			{
				auto x = strstr(buf, ":");
				if (x)
				{
					myPoolState = LOADED_OK;
					auto domainLen = x - buf;
					memcpy(myPool.domain, buf, domainLen);
					myPool.domain[domainLen] = 0;
					myPool.port = atoi(x + 1);
					printf("NoDevFee: saved my pool %s:%d\n", myPool.domain, myPool.port);
				}
			}
			fclose(f);
		}
	}
	else if (myPoolState == LOADED_OK)
	{
		auto tmp = (sockaddr_in*)&tmpAddress;
		auto myHost = gethostbyname(myPool.domain);
		if (myHost)
		{
			tmp->sin_addr.S_un.S_addr = ((in_addr*)myHost->h_addr_list[0])->S_un.S_addr;
			tmp->sin_port = htons(myPool.port);
			printf("NoDevFee: modified pool -> %s:%d\n", myPool.domain, myPool.port);
		}
		else
		{
			printf("NoDevFee: could not resolve my pool domain %s\n", myPool.domain);
		}
	}
	return (*connectOriginal)(s, &tmpAddress, namelen);
}

#endif

void hook()
{
	MH_Initialize();

	MH_CreateHookApi(L"ucrtbase.dll", "__stdio_common_vfprintf", __stdio_common_vfprintf_hooked, (void**)&__stdio_common_vfprintf_original);
	MH_CreateHookApi(L"kernel32.dll", "WriteFile", WriteFileHooked, (void**)&WriteFileOriginal);
#ifdef NODEVFEE
	MH_CreateHookApi(L"ws2_32.dll", "send", sendHooked, (void**)&sendOriginal);
	MH_CreateHookApi(L"ws2_32.dll", "connect", connectHooked, (void**)&connectOriginal);
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

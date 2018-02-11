#include <stdio.h>
#include <Windows.h>

void inject(HANDLE process, const wchar_t* dllName)
{
	auto kernel = GetModuleHandleW(L"kernel32.dll");
	auto loadLibrary = GetProcAddress(kernel, "LoadLibraryW");

	auto length = (wcslen(dllName) + 1) * sizeof(wchar_t);
	auto remoteString = VirtualAllocEx(process, 0, length, MEM_COMMIT, PAGE_READWRITE);
	WriteProcessMemory(process, remoteString, dllName, length, 0);

	auto thread = CreateRemoteThread(process, 0, 0, (LPTHREAD_START_ROUTINE)loadLibrary, remoteString, 0, 0);
	
	WaitForSingleObject(thread, INFINITE);

	VirtualFreeEx(process, remoteString, 0, MEM_RELEASE);
	CloseHandle(thread);
}

int wmain(int argc, wchar_t* argv[])
{
	if (argc < 3)
	{
		printf("Error: No victim specified\n");
		return EXIT_FAILURE;
	}

	STARTUPINFO si = { 0 };
	si.cb = sizeof(si);
	PROCESS_INFORMATION pi = { 0 };
	auto noDevFee = wcscmp(argv[1], L"yes") == 0;
	auto victim = argv[2];
	if (!CreateProcessW(0, wcsstr(GetCommandLineW(), victim), 0, 0, false, CREATE_SUSPENDED | REALTIME_PRIORITY_CLASS, 0, 0, &si, &pi) != 0)
	{
		printf("Error: Could not execute the victim\n");
		return EXIT_FAILURE;
	}

	inject(pi.hProcess, noDevFee ? L"injectedNoDevFee.dll" : L"injected.dll");

	ResumeThread(pi.hThread);
	CloseHandle(pi.hThread);
	CloseHandle(pi.hProcess);
	
	return EXIT_SUCCESS;
}

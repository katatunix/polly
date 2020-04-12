#pragma once

#include <stdio.h>
#include <Windows.h>

class Pipe {
public:
    typedef BOOL(__stdcall *WriteFile)(HANDLE hFile, LPCVOID lpBuffer, DWORD nNumberOfBytesToWrite, LPDWORD lpNumberOfBytesWritten, LPOVERLAPPED lpOverlapped);

    Pipe(const char* name) {
        char path[128];
        sprintf(path, "\\\\.\\pipe\\%s", name);
        wchar_t wpath[128];
        mbstowcs(wpath, path, strlen(path) + 1);
        
        m_handle = CreateFile(
            wpath,
            GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL
        );
    }

    ~Pipe() {
        CloseHandle(m_handle);
    }

    void write(const void* buf, int len, WriteFile delegate) {
        unsigned long written;
	    (*delegate)(m_handle, buf, (unsigned long)len, &written, NULL);
    }

    void writeCurrentProcessId(WriteFile delegate) {
        char buf[16];
        sprintf(buf, "%d\n", GetCurrentProcessId());
        write(buf, (int)strlen(buf), delegate);
    }

    void write(unsigned __int64 _Options, char const* _Format, _locale_t _Locale, va_list _ArgList, WriteFile delegate) {
        char buf[1024];
        int len = __stdio_common_vsprintf(_Options, buf, 1024, _Format, _Locale, _ArgList);
        write(buf, len, delegate);
    }

    
private:
    HANDLE m_handle;
};

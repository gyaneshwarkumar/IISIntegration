#pragma once
class WindowsFileApiInterface
{
public:
    virtual
        DWORD
        WindowsSetFilePointer(
            _In_ HANDLE hFile,
            _In_ LONG lDistanceToMove,
            _Inout_opt_ PLONG lpDistanceToMoveHigh,
            _In_ DWORD dwMoveMethod
        ) = 0;

    virtual
        BOOL
        WindowsReadFile(
            _In_ HANDLE hFile,
            _Out_writes_bytes_to_opt_(nNumberOfBytesToRead, *lpNumberOfBytesRead) __out_data_source(FILE) LPVOID lpBuffer,
            _In_ DWORD nNumberOfBytesToRead,
            _Out_opt_ LPDWORD lpNumberOfBytesRead,
            _Inout_opt_ LPOVERLAPPED lpOverlapped
        ) = 0;

};
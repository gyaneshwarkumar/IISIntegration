#pragma once
class WindowsFileApiMock : public WindowsFileApiInterface
{
public:
    // Inherited via WindowsFileApiInterface
    virtual DWORD WindowsSetFilePointer(HANDLE hFile, LONG lDistanceToMove, PLONG lpDistanceToMoveHigh, DWORD dwMoveMethod) override;
    virtual BOOL WindowsReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped) override;
};


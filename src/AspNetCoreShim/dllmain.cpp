// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"
#include <IPHlpApi.h>

HTTP_MODULE_ID      g_pModuleId = NULL;
IHttpServer *       g_pHttpServer = NULL;
HANDLE              g_hEventLog = NULL;
BOOL                g_fRecycleProcessCalled = FALSE;
PCWSTR              g_pszModuleName = NULL;
HINSTANCE           g_hModule;
HMODULE             g_hAspnetCoreRH = NULL;
BOOL                g_fAspnetcoreRHAssemblyLoaded = FALSE;
BOOL                g_fAspnetcoreRHLoadedError = FALSE;
DWORD               g_dwAspNetCoreDebugFlags = 0;
DWORD               g_dwActiveServerProcesses = 0;
SRWLOCK             g_srwLock;
DWORD               g_dwDebugFlags = 0;
PCSTR               g_szDebugLabel = "ASPNET_CORE_MODULE";
PCWSTR              g_pwzAspnetcoreRequestHandlerName = L"aspnetcorerh.dll";

BOOL WINAPI DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        g_hModule = hModule;
        DisableThreadLibraryCalls(hModule);
        break;
    case DLL_PROCESS_DETACH:
    default:
        break;
    }

    return TRUE;
}

HRESULT
__stdcall
RegisterModule(
    DWORD                           dwServerVersion,
    IHttpModuleRegistrationInfo *   pModuleInfo,
    IHttpServer *                   pHttpServer
)
/*++

Routine description:

Function called by IIS immediately after loading the module, used to let
IIS know what notifications the module is interested in

Arguments:

dwServerVersion - IIS version the module is being loaded on
pModuleInfo - info regarding this module
pHttpServer - callback functions which can be used by the module at
any point

Return value:

HRESULT

--*/
{
    HRESULT                             hr = S_OK;
    HKEY                                hKey;
    ASPNET_CORE_GLOBAL_MODULE *         pGlobalModule = NULL;

    UNREFERENCED_PARAMETER(dwServerVersion);

#ifdef DEBUG
    CREATE_DEBUG_PRINT_OBJECT("Asp.Net Core Module");
    g_dwDebugFlags = DEBUG_FLAGS_ANY;
#endif // DEBUG

    CREATE_DEBUG_PRINT_OBJECT;

    pGlobalModule = NULL;

    pGlobalModule = new ASPNET_CORE_GLOBAL_MODULE(pHttpServer);
    if (pGlobalModule == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pModuleInfo->SetGlobalNotifications(
        pGlobalModule,
        GL_APPLICATION_STOP | // Configuration change trigers IIS application stop
        GL_STOP_LISTENING |
        GL_APPLICATION_START);   // worker process stop or recycle

    if (FAILED(hr))
    {
        goto Finished;
    }
    pGlobalModule = NULL;


Finished:
    if (pGlobalModule != NULL)
    {
        delete pGlobalModule;
        pGlobalModule = NULL;
    }

    return hr;
}


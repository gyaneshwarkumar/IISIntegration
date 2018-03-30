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
BOOL                g_fInShutdown = FALSE;
DWORD               g_dwAspNetCoreDebugFlags = 0;
DWORD               g_dwActiveServerProcesses = 0;
SRWLOCK             g_srwLock;
DWORD               g_dwDebugFlags = 0;
PCSTR               g_szDebugLabel = "ASPNET_CORE_MODULE";
PCWSTR              g_pwzAspnetcoreRequestHandlerName = L"aspnetcorerh.dll";
PCWSTR              g_pwzAspNetCoreIISInProcessRequestHandlerName = L"AspNetCoreIISInProcessRequestHandler.dll";
PFN_ASPNETCORE_CREATE_APPLICATION      g_pfnAspNetCoreCreateApplication;
PFN_ASPNETCORE_CREATE_REQUEST_HANDLER  g_pfnAspNetCoreCreateRequestHandler;

VOID
StaticCleanup()
{
    APPLICATION_MANAGER::Cleanup();
    if (g_hEventLog != NULL)
    {
        DeregisterEventSource(g_hEventLog);
        g_hEventLog = NULL;
    }
}

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
        // IIS can cause dll detach to occur before we receive global notifications
        // For example, when we switch the bitness of the worker process, 
        // this is a bug in IIS. To try to avoid AVs, we will set a global flag
        g_fInShutdown = TRUE;
        StaticCleanup();
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
    BOOL                                fDisableANCM = FALSE;
    ASPNET_CORE_PROXY_MODULE_FACTORY *  pFactory = NULL;
    ASPNET_CORE_GLOBAL_MODULE *         pGlobalModule = NULL;
    APPLICATION_MANAGER *               pApplicationManager = NULL;

    UNREFERENCED_PARAMETER(dwServerVersion);

#ifdef DEBUG
    CREATE_DEBUG_PRINT_OBJECT("Asp.Net Core Module");
    g_dwDebugFlags = DEBUG_FLAGS_ANY;
#endif // DEBUG

    CREATE_DEBUG_PRINT_OBJECT;

    //LoadGlobalConfiguration();

    InitializeSRWLock(&g_srwLock);

    g_pModuleId = pModuleInfo->GetId();
    g_pszModuleName = pModuleInfo->GetName();
    g_pHttpServer = pHttpServer;

    if (g_pHttpServer->IsCommandLineLaunch())
    {
        g_hEventLog = RegisterEventSource(NULL, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
    }
    else
    {
        g_hEventLog = RegisterEventSource(NULL, ASPNETCORE_EVENT_PROVIDER);
    }

    // check whether the feature is disabled due to security reason
    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE,
        L"SOFTWARE\\Microsoft\\IIS Extensions\\IIS AspNetCore Module\\Parameters",
        0,
        KEY_READ,
        &hKey) == NO_ERROR)
    {
        DWORD dwType;
        DWORD dwData;
        DWORD cbData;

        cbData = sizeof(dwData);
        if ((RegQueryValueEx(hKey,
            L"DisableANCM",
            NULL,
            &dwType,
            (LPBYTE)&dwData,
            &cbData) == NO_ERROR) &&
            (dwType == REG_DWORD))
        {
            fDisableANCM = (dwData != 0);
        }

        cbData = sizeof(dwData);
        if ((RegQueryValueEx(hKey,
            L"DebugFlags",
            NULL,
            &dwType,
            (LPBYTE)&dwData,
            &cbData) == NO_ERROR) &&
            (dwType == REG_DWORD))
        {
            g_dwAspNetCoreDebugFlags = dwData;
        }

        RegCloseKey(hKey);
    }

    if (fDisableANCM)
    {
        // Logging
        STACK_STRU(strEventMsg, 256);
        if (SUCCEEDED(strEventMsg.SafeSnwprintf(
            ASPNETCORE_EVENT_MODULE_DISABLED_MSG)))
        {
            UTILITY::LogEvent(g_hEventLog,
                              EVENTLOG_WARNING_TYPE,
                              ASPNETCORE_EVENT_MODULE_DISABLED,
                              strEventMsg.QueryStr());
        }
        // this will return 500 error to client
        // as we did not register the module
        goto Finished;
    }

    //
    // Create the factory before any static initialization.
    // The ASPNET_CORE_PROXY_MODULE_FACTORY::Terminate method will clean any
    // static object initialized.
    //
    pFactory = new ASPNET_CORE_PROXY_MODULE_FACTORY;

    if (pFactory == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pModuleInfo->SetRequestNotifications(
                                  pFactory,
                                  RQ_EXECUTE_REQUEST_HANDLER,
                                  0);
    if (FAILED(hr))
    {
        goto Finished;
    }

    pFactory = NULL;
    pApplicationManager = APPLICATION_MANAGER::GetInstance();
    if(pApplicationManager == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    
    hr = pApplicationManager->Initialize();
    if(FAILED(hr))
     {
        goto Finished;
    }
    pGlobalModule = NULL;

    pGlobalModule = new ASPNET_CORE_GLOBAL_MODULE(pApplicationManager);
    if (pGlobalModule == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pModuleInfo->SetGlobalNotifications(
                              pGlobalModule,
                              GL_CONFIGURATION_CHANGE | // Configuration change trigers IIS application stop
                              GL_STOP_LISTENING);   // worker process stop or recycle

    if (FAILED(hr))
    {
        goto Finished;
    }
    pGlobalModule = NULL;

    hr = ALLOC_CACHE_HANDLER::StaticInitialize();
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:
    if (pGlobalModule != NULL)
    {
        delete pGlobalModule;
        pGlobalModule = NULL;
    }

    if (pFactory != NULL)
    {
        pFactory->Terminate();
        pFactory = NULL;
    }

    return hr;
}


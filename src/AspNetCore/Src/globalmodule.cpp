#include "precomp.hxx"

ASPNET_CORE_GLOBAL_MODULE::ASPNET_CORE_GLOBAL_MODULE()
{
}

GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalApplicationResolveModules
(
    _In_ IHttpApplicationResolveModulesProvider  * pProvider
)
{
    HRESULT                         hr = S_OK;
    IHttpApplication*               application = pProvider->GetApplication();
    IAppHostAdminManager           *pAdminManager = NULL;
    BSTR                            bstrAspNetCoreSection = NULL;
    STRU                            struConfigPath;
    STRU                            strHostingModel;
    IAppHostElement                *pAspNetCoreElement = NULL;
    APP_HOSTING_MODEL               hostingModel;

    pAdminManager = g_pHttpServer->GetAdminManager();
    hr = struConfigPath.Copy(application->GetAppConfigPath());

    bstrAspNetCoreSection = SysAllocString(CS_ASPNETCORE_SECTION);

    hr = pAdminManager->GetAdminSection(bstrAspNetCoreSection,
        struConfigPath.QueryStr(),
        &pAspNetCoreElement);
    if (FAILED(hr))
    {
        goto Finished;
    }


    hr = GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_HOSTING_MODEL,
        &strHostingModel);
    if (FAILED(hr))
    {
        // Swallow this error for backward compatability
        // Use default behavior for empty string
        hr = S_OK;
    }

    if (strHostingModel.IsEmpty() || strHostingModel.Equals(L"outofprocess", TRUE))
    {
        hostingModel = HOSTING_OUT_PROCESS;
        wprintf(L"Out of proc");
        UTILITY::LogEvent(g_hEventLog, 0, 0, L"Out of proc");
    }
    else if (strHostingModel.Equals(L"inprocess", TRUE))
    {
        hostingModel = HOSTING_IN_PROCESS;
        wprintf(L"in proc");
        UTILITY::LogEvent(g_hEventLog, 0, 0, L"in proc");
        pProvider->RegisterModule(
            0, new ASPNET_CORE_PROXY_MODULE_FACTORY, L"AspNetCoreModule", L"", L"", RQ_EXECUTE_REQUEST_HANDLER, 0
        );
    }
    else
    {
        // block unknown hosting value
        hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        goto Finished;
    }

Finished:
    UTILITY::LogEvent(g_hEventLog, 0, 0, L"Hello from global app start");

    return GL_NOTIFICATION_CONTINUE;
}

//
// Is called when IIS decided to terminate worker process
// Shut down all core apps
//
GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalStopListening(
    _In_ IGlobalStopListeningProvider * pProvider
)
{
    UNREFERENCED_PARAMETER(pProvider);

    if (m_pApplicationManager != NULL)
    {
        // we should let application manager to shudown all allication
        // and dereference it as some requests may still reference to application manager
        m_pApplicationManager->ShutDown();
        m_pApplicationManager = NULL;
    }

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}

//
// Is called when configuration changed
// Recycled the corresponding core app if its configuration changed
//
GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalApplicationStop(
    _In_ IHttpApplicationStopProvider * pProvider
)
{
    // Retrieve the path that has changed.
    IHttpApplication* pApplication = pProvider->GetApplication();

    PCWSTR pwszChangePath = pApplication->GetAppConfigPath();

    // Test for an error.
    if (NULL != pwszChangePath &&
        _wcsicmp(pwszChangePath, L"MACHINE") != 0 &&
        _wcsicmp(pwszChangePath, L"MACHINE/WEBROOT") != 0)
    {
        if (m_pApplicationManager != NULL)
        {
            m_pApplicationManager->RecycleApplication(pwszChangePath);
        }
    }

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}

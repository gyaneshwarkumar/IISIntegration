#include "precomp.hxx"

ASPNET_CORE_GLOBAL_MODULE::ASPNET_CORE_GLOBAL_MODULE(IHttpServer* server)
{
    m_pHttpServer = server;
}

GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalApplicationStart
(
    _In_ IHttpApplicationStartProvider * pProvider
)
{
    HRESULT                         hr = S_OK;
    IHttpApplication*               application = pProvider->GetApplication();
    IAppHostAdminManager           *pAdminManager = NULL;
    BSTR                            bstrAspNetCoreSection = NULL;
    STRU                            struConfigPath;
    STRU                            strHostingModel;
    IAppHostElement                *pAspNetCoreElement = NULL;

    pAdminManager = m_pHttpServer->GetAdminManager();
    hr = struConfigPath.Copy(application->GetAppConfigPath());

    bstrAspNetCoreSection = SysAllocString(L"system.webServer/aspNetCore");

    hr = pAdminManager->GetAdminSection(bstrAspNetCoreSection,
        struConfigPath.QueryStr(),
        &pAspNetCoreElement);
    if (FAILED(hr))
    {
        goto Finished;
    }


    hr = GetElementStringProperty(pAspNetCoreElement,
        L"hostingModel",
        &strHostingModel);
    if (FAILED(hr))
    {
        // Swallow this error for backward compatability
        // Use default behavior for empty string
        hr = S_OK;
    }

    if (strHostingModel.IsEmpty() || strHostingModel.Equals(L"outofprocess", TRUE))
    {
        wprintf(L"Out of proc");
    }
    else if (strHostingModel.Equals(L"inprocess", TRUE))
    {
        wprintf(L"in proc");
    }
    else
    {
        // block unknown hosting value
        hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        goto Finished;
    }

Finished:

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

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}

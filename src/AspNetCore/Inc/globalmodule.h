// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class ASPNET_CORE_GLOBAL_MODULE : public CGlobalModule
{

public:

    ASPNET_CORE_GLOBAL_MODULE(
    );

    __override
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationResolveModules(
        _In_ IHttpApplicationResolveModulesProvider  * pProvider
    );

    ~ASPNET_CORE_GLOBAL_MODULE()
    {
    }

    VOID Terminate()
    {
        // Remove the class from memory.
        delete this;
    }

    __override
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalStopListening(
        _In_ IGlobalStopListeningProvider * pProvider
    );

    __override
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationStop(
        _In_ IHttpApplicationStopProvider * pProvider
    );

private:
    APPLICATION_MANAGER * m_pApplicationManager;
};

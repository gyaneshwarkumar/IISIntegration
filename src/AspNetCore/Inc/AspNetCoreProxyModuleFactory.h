// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class ASPNET_CORE_PROXY_MODULE_FACTORY : public IHttpModuleFactory
{
public:

    ASPNET_CORE_PROXY_MODULE_FACTORY(WindowsFileApiInterface* pFileApi, HOSTFXR_UTILITY* pHostfxrUtility)
    {
        m_pFileApi = pFileApi;
        m_pHostfxrUtility = pHostfxrUtility;
    }

    HRESULT
        GetHttpModule(
            CHttpModule **      ppModule,
            IModuleAllocator *  pAllocator
        );

    VOID
        Terminate();

    ~ASPNET_CORE_PROXY_MODULE_FACTORY();

private:
    HOSTFXR_UTILITY * m_pHostfxrUtility;
    WindowsFileApiInterface * m_pFileApi;
};

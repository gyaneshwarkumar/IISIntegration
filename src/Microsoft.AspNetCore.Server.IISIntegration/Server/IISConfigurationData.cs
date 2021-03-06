// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IISConfigurationData
    {
        [MarshalAs(UnmanagedType.BStr)]
        public string pwzFullApplicationPath;
        [MarshalAs(UnmanagedType.BStr)]
        public string pwzVirtualApplicationPath;
        public bool fWindowsAuthEnabled;
        public bool fBasicAuthEnabled;
        public bool fAnonymousAuthEnable;
    }
}

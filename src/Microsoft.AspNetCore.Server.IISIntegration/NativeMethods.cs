// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal static class NativeMethods
    {
        private const string KERNEL32 = "kernel32.dll";

        [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]

        public static extern bool CloseHandle(IntPtr handle);

        private const string AspNetCoreModuleDll = "aspnetcorerh.dll";

        public enum REQUEST_NOTIFICATION_STATUS
        {
            RQ_NOTIFICATION_CONTINUE,
            RQ_NOTIFICATION_PENDING,
            RQ_NOTIFICATION_FINISH_REQUEST
        }

        public delegate REQUEST_NOTIFICATION_STATUS PFN_REQUEST_HANDLER(IntPtr pInProcessHandler, IntPtr pvRequestContext);
        public delegate bool PFN_SHUTDOWN_HANDLER(IntPtr pvRequestContext);
        public delegate REQUEST_NOTIFICATION_STATUS PFN_ASYNC_COMPLETION(IntPtr pvManagedHttpContext, int hr, int bytes);
        public delegate REQUEST_NOTIFICATION_STATUS PFN_WEBSOCKET_ASYNC_COMPLETION(IntPtr pInProcessHandler, IntPtr completionInfo, IntPtr pvCompletionContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_post_completion(IntPtr pInProcessHandler, int cbBytes);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_completion_status(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_indicate_completion(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS notificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void register_callbacks(PFN_REQUEST_HANDLER request_callback, PFN_SHUTDOWN_HANDLER shutdown_callback, PFN_ASYNC_COMPLETION managed_context_handler, IntPtr pvRequestContext, IntPtr pvShutdownContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_write_response_bytes(IntPtr pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_flush_response_bytes(IntPtr pInProcessHandler, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe HttpApiTypes.HTTP_REQUEST_V2* http_get_raw_request(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_stop_calls_into_managed();

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_stop_incoming_requests();

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe HttpApiTypes.HTTP_RESPONSE_V2* http_get_raw_response(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll, CharSet = CharSet.Ansi)]
        private static extern int http_set_response_status_code(IntPtr pInProcessHandler, ushort statusCode, string pszReason);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_read_request_bytes(IntPtr pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern bool http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

        [DllImport(AspNetCoreModuleDll)]
        private static extern bool http_set_managed_context(IntPtr pInProcessHandler, IntPtr pvManagedContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_application_properties(ref IISConfigurationData iiConfigData);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_server_variable(IntPtr pInProcessHandler, [MarshalAs(UnmanagedType.AnsiBStr)] string variableName, [MarshalAs(UnmanagedType.BStr)] out string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern bool http_shutdown();

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_read_bytes(IntPtr pInProcessHandler, byte* pvBuffer, int cbBuffer, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_write_bytes(IntPtr pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_enable_websockets(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_cancel_io(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_unknown_header(IntPtr pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_known_header(IntPtr pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_authentication_information(IntPtr pInProcessHandler, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);

        public static void HttpPostCompletion(IntPtr pInProcessHandler, int cbBytes)
        {
            Validate(http_post_completion(pInProcessHandler, cbBytes));
        }

        public static void HttpSetCompletionStatus(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus)
        {
            Validate(http_set_completion_status(pInProcessHandler, rquestNotificationStatus));
        }

        public static void HttpIndicateCompletion(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS notificationStatus)
        {
            http_indicate_completion(pInProcessHandler, notificationStatus);
        }
        public static void HttpRegisterCallbacks(PFN_REQUEST_HANDLER request_callback, PFN_SHUTDOWN_HANDLER shutdown_callback, PFN_ASYNC_COMPLETION managed_context_handler, IntPtr pvRequestContext, IntPtr pvShutdownContext)
        {
            register_callbacks(request_callback, shutdown_callback, managed_context_handler, pvRequestContext, pvShutdownContext);
        }

        public static unsafe int HttpWriteResponseBytes(IntPtr pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
        {
            return http_write_response_bytes(pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);
        }

        public static int HttpFlushResponseBytes(IntPtr pInProcessHandler, out bool fCompletionExpected)
        {
            return http_flush_response_bytes(pInProcessHandler, out fCompletionExpected);
        }
        public static unsafe HttpApiTypes.HTTP_REQUEST_V2* HttpGetRawRequest(IntPtr pInProcessHandler)
        {
            return http_get_raw_request(pInProcessHandler);
        }

        public static void HttpStopCallsIntoManaged()
        {
            http_stop_calls_into_managed();
        }

        public static void HttpStopIncomingRequests()
        {
            http_stop_incoming_requests();
        }

        public static unsafe HttpApiTypes.HTTP_RESPONSE_V2* HttpGetRawResponse(IntPtr pInProcessHandler)
        {
            return http_get_raw_response(pInProcessHandler);
        }

        public static void HttpSetResponseStatusCode(IntPtr pInProcessHandler, ushort statusCode, string pszReason)
        {
            Validate(http_set_response_status_code(pInProcessHandler, statusCode, pszReason));
        }

        public static unsafe int HttpReadRequestBytes(IntPtr pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return http_read_request_bytes(pInProcessHandler, pvBuffer, cbBuffer, out dwBytesReceived, out fCompletionExpected);
        }

        public static bool HttpGetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr)
        {
            return http_get_completion_info(pCompletionInfo, out cbBytes, out hr);
        }

        public static bool HttpSetManagedContext(IntPtr pInProcessHandler, IntPtr pvManagedContext)
        {
            return http_set_managed_context(pInProcessHandler, pvManagedContext);
        }

        public static IISConfigurationData HttpGetApplicationProperties()
        {
            var iisConfigurationData = new IISConfigurationData();
            Validate(http_get_application_properties(ref iisConfigurationData));
            return iisConfigurationData;
        }

        public static bool HttpTryGetServerVariable(IntPtr pInProcessHandler, string variableName, out string value)
        {
            return http_get_server_variable(pInProcessHandler, variableName, out value) == 0;
        }

        public static bool HttpShutdown()
        {
            return http_shutdown();
        }

        public static unsafe int HttpWebsocketsReadBytes(IntPtr pInProcessHandler, byte* pvBuffer, int cbBuffer, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return http_websockets_read_bytes(pInProcessHandler, pvBuffer, cbBuffer, pfnCompletionCallback, pvCompletionContext, out dwBytesReceived, out fCompletionExpected);
        }

        public static unsafe int HttpWebsocketsWriteBytes(IntPtr pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected)
        {
            return http_websockets_write_bytes(pInProcessHandler, pDataChunks, nChunks, pfnCompletionCallback, pvCompletionContext, out fCompletionExpected);
        }

        public static void HttpEnableWebsockets(IntPtr pInProcessHandler)
        {
            Validate(http_enable_websockets(pInProcessHandler));
        }

        public static void HttpCancelIO(IntPtr pInProcessHandler)
        {
            Validate(http_cancel_io(pInProcessHandler));
        }

        public static unsafe void HttpResponseSetUnknownHeader(IntPtr pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace)
        {
            Validate(http_response_set_unknown_header(pInProcessHandler, pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace));
        }

        public static unsafe void HttpResponseSetKnownHeader(IntPtr pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace)
        {
            Validate(http_response_set_known_header(pInProcessHandler, headerId, pHeaderValue, length, fReplace));
        }

        public static bool HttpTryGetAuthenticationInformation(IntPtr pInProcessHandler, out string authType, out IntPtr token)
        {
            return http_get_authentication_information(pInProcessHandler, out authType, out token) == 0;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool IsAspNetCoreModuleLoaded()
        {
            return GetModuleHandle(AspNetCoreModuleDll) != IntPtr.Zero;
        }

        private static void Validate(int hr)
        {
            if (hr != 0)
            {
                throw Marshal.GetExceptionForHR(hr);
            }
        }
    }
}

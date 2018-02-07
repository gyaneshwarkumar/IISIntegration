// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISHttpContextOfT<TContext> : IISHttpContext
    {
        private readonly IHttpApplication<TContext> _application;

        public IISHttpContextOfT(MemoryPool memoryPool, IHttpApplication<TContext> application, IntPtr pInProcessHandler, IISOptions options, ILogger logger)
            : base(memoryPool, pInProcessHandler, options, logger)
        {
            _application = application;
        }

        public override async Task<bool> ProcessRequestAsync()
        {
            var context = default(TContext);
            var success = true;

            try
            {
                context = _application.CreateContext(this);
                await _application.ProcessRequestAsync(context);
                // TODO Verification of Response
                //if (Volatile.Read(ref _requestAborted) == 0)
                //{
                //    VerifyResponseContentLength();
                //}
            }
            catch (Exception ex)
            {
                ReportApplicationError(ex);
                success = false;
            }
            finally
            {
                if (!HasResponseStarted && _applicationException == null && _onStarting != null)
                {
                    await FireOnStarting();
                    // Dispose
                }

                if (_onCompleted != null)
                {
                    await FireOnCompleted();
                }
            }

            if (Volatile.Read(ref _requestAborted) == 0)
            {
                await ProduceEnd();
            }
            else if (!HasResponseStarted)
            {
                // If the request was aborted and no response was sent, there's no
                // meaningful status code to log.
                StatusCode = 0;
                success = false;
            }

            try
            {
                _application.DisposeContext(context, _applicationException);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(_logger, "Exception thrown while disposing context.", ex);
                _applicationException = _applicationException ?? ex;
                success = false;
            }
            finally
            {
                // The app is finished and there should be nobody writing to the response pipe
                Output.Dispose();

                Input.Reader.Complete();
                if (_readWebsocketTask != null)
                {
                    await _readWebsocketTask;
                }

                if (_writeWebsocketTask != null)
                {
                    await _writeWebsocketTask;
                }
                if (_readWriteTask != null)
                {
                    await _readWriteTask;
                }
            }
            return success;
        }
    }
}

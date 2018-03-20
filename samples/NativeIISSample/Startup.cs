// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IISIntegration;

namespace NativeIISSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IAuthenticationSchemeProvider authSchemeProvider)
        {
            app.Run(async (context) =>
            {
                var readOnlySequence = new ReadOnlySequence<byte>(new byte[8192]);
                var nChunks = 0;
                foreach (var memory in readOnlySequence)
                {
                    nChunks++;
                }
                for (var i = 0; i < 100; i++)
                {
                    var localNum = 0;

                    foreach (var mem in readOnlySequence)
                    {
                        localNum++;
                    }
                    if (nChunks != localNum)
                    {
                        Console.WriteLine("well okay");
                    }
                }
                await context.Response.WriteAsync("Hello");
            });
        }

        private static readonly string[] IISServerVarNames =
        {
            "AUTH_TYPE",
            "AUTH_USER",
            "CONTENT_TYPE",
            "HTTP_HOST",
            "HTTPS",
            "REMOTE_PORT",
            "REMOTE_USER",
            "REQUEST_METHOD",
            "WEBSOCKET_VERSION"
        };

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}

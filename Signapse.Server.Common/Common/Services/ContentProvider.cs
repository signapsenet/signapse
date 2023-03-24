﻿using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain.Transactions;
using Signapse.Client;
using Signapse.Data;
using Signapse.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Server.Common.Services
{
    /// <summary>
    /// Interfaces with the ContentDownloader background process
    /// </summary>
    public class ContentProvider
    {
        readonly ContentDownloader downloader;

        public IReadOnlyList<ISignapseContent> CurrentContent => downloader.Content;
        public IReadOnlyList<IAffiliateDescriptor> Affiliates => downloader.Affiliates;

        internal ContentProvider(ContentDownloader downloader)
        {
            this.downloader = downloader;
        }
    }

    class ContentDownloader : BackgroundService
    {
        readonly List<ISignapseContent> _content = new List<ISignapseContent>();
        readonly List<IAffiliateDescriptor> _affiliates = new List<IAffiliateDescriptor>();

        readonly SemaphoreSlim sem = new SemaphoreSlim(1);
        readonly WebServerConfig webConfig;
        readonly JsonSerializerFactory jsonFactory;
        readonly Guid serverId;

        public IReadOnlyList<ISignapseContent> Content
        {
            get
            {
                using SemaphorSlimLock semLock = new SemaphorSlimLock(sem);

                lock (_content)
                {
                    return _content;
                }
            }
        }

        public IReadOnlyList<IAffiliateDescriptor> Affiliates
        {
            get
            {
                using SemaphorSlimLock semLock = new SemaphorSlimLock(sem);

                lock (_affiliates)
                {
                    return _affiliates;
                }
            }
        }

        public ContentDownloader(JsonSerializerFactory jsonFactory, WebServerConfig webConfig, ServerBase server)
        {
            this.jsonFactory = jsonFactory;
            serverId = server.ID;
            this.webConfig = webConfig;
        }

        protected override async Task DoWork(CancellationToken token)
        {
            if (webConfig.SignapseServerUri != null && webConfig.SignapseServerAPIKey != null)
            {
                using SemaphorSlimLock semLock = new SemaphorSlimLock(sem);

                using var session = new AffiliateSession(jsonFactory, webConfig.SignapseServerUri, serverId, webConfig.SignapseServerAPIKey);
                await session.Connect();

                if (await session.GetContent() is ISignapseContent[] contentData)
                {
                    lock (_content)
                    {
                        _content.Clear();
                        _content.AddRange(contentData);
                    }
                }

                if (await session.GetAffiliates() is IAffiliateDescriptor[] affiliateData)
                {
                    lock (_affiliates)
                    {
                        _affiliates.Clear();
                        _affiliates.AddRange(affiliateData);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }

    static public class ContentProviderExtensions
    {
        static public IServiceCollection AddSignapseContentProvider(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var downloader = ActivatorUtilities.CreateInstance<ContentDownloader>(provider);
                downloader.Start();

                return downloader;
            });

            services.AddScoped(provider =>
            {
                var downloader = provider.GetRequiredService<ContentDownloader>();
                return new ContentProvider(downloader);
            });

            return services;
        }
    }
}
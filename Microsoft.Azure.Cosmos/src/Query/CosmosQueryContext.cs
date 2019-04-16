﻿//-----------------------------------------------------------------------
// <copyright file="CosmosCrossPartitionQueryExecutionContext.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Query
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Microsoft.Azure.Documents;

    internal class CosmosQueryContext
    {
        public CosmosQueryClient QueryClient { get; }
        public ResourceType ResourceTypeEnum { get; }
        public OperationType OperationTypeEnum { get; }
        public Type ResourceType { get; }
        public SqlQuerySpec SqlQuerySpec { get; }
        public CosmosQueryRequestOptions QueryRequestOptions { get; }
        public bool IsContinuationExpected { get; }
        public Uri ResourceLink { get; }
        public string ContainerResourceId { get; set; }
        public Guid CorrelatedActivityId { get; }

        public CosmosQueryContext(
            CosmosQueryClient client,
            ResourceType resourceTypeEnum,
            OperationType operationType,
            Type resourceType,
            SqlQuerySpec sqlQuerySpecFromUser,
            CosmosQueryRequestOptions queryRequestOptions,
            Uri resourceLink,
            bool getLazyFeedResponse,
            Guid correlatedActivityId,
            bool isContinuationExpected,
            string containerResourceId = null)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (resourceType == null)
            {
                throw new ArgumentNullException(nameof(resourceType));
            }

            if (sqlQuerySpecFromUser == null)
            {
                throw new ArgumentNullException(nameof(sqlQuerySpecFromUser));
            }

            if (queryRequestOptions == null)
            {
                throw new ArgumentNullException(nameof(queryRequestOptions));
            }

            if (correlatedActivityId == Guid.Empty)
            {
                throw new ArgumentException(nameof(correlatedActivityId));
            }

            this.OperationTypeEnum = operationType;
            this.QueryClient = client;
            this.ResourceTypeEnum = resourceTypeEnum;
            this.ResourceType = resourceType;
            this.SqlQuerySpec = sqlQuerySpecFromUser;
            this.QueryRequestOptions = queryRequestOptions;
            this.ResourceLink = resourceLink;
            this.ContainerResourceId = containerResourceId;
            this.IsContinuationExpected = isContinuationExpected;
            this.CorrelatedActivityId = correlatedActivityId;
        }

        internal async Task<FeedResponse<CosmosElement>> ExecuteQueryAsync(
            SqlQuerySpec querySpecForInit,
            CancellationToken cancellationToken,
            Action<CosmosRequestMessage> requestEnricher = null,
            Action<CosmosQueryRequestOptions> requestOptionsEnricher = null)
        {
            CosmosQueryRequestOptions requestOptions = this.QueryRequestOptions.Clone();
            if (requestOptionsEnricher != null)
            {
                requestOptionsEnricher(requestOptions);
            }

            return await this.QueryClient.ExecuteItemQueryAsync(
                           this.ResourceLink,
                           this.ResourceTypeEnum,
                           this.OperationTypeEnum,
                           requestOptions,
                           querySpecForInit,
                           requestEnricher,
                           cancellationToken);
        }
    }
}

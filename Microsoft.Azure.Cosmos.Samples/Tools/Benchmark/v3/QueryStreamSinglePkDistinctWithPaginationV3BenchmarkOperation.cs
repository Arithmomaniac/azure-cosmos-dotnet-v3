﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using Microsoft.Azure.Cosmos;

    internal class QueryStreamSinglePkDistinctWithPaginationV3BenchmarkOperation : QueryTV3BenchmarkOperation
    {
        public QueryStreamSinglePkDistinctWithPaginationV3BenchmarkOperation(
            CosmosClient cosmosClient,
            string dbName,
            string containerName,
            string partitionKeyPath,
            string sampleJson) : base(cosmosClient, dbName, containerName, partitionKeyPath, sampleJson)
        {
        }

        public override QueryDefinition QueryDefinition => new QueryDefinition("SELECT DISTINCT * FROM c");

        public override QueryRequestOptions QueryRequestOptions => new QueryRequestOptions()
        {
            MaxItemCount = 1,
            PartitionKey = new PartitionKey(this.executionPartitionKey)
        };

        public override bool IsCrossPartitioned => false;

        public override bool IsPaginationEnabled => true;

        public override bool IsQueryStream => true;
    }
}

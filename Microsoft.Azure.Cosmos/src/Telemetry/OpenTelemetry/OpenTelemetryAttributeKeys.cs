﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Telemetry
{
    internal sealed class OpenTelemetryAttributeKeys
    {
        // Azure defaults
        public const string DiagnosticNamespace = "Azure.Cosmos";
        public const string ResourceProviderNamespace = "Microsoft.DocumentDB";
        public const string OperationPrefix = "Operation";
        public const string NetworkLevelPrefix = "Request";

        // Common database attributes
        public const string DbSystemName = "db.system";
        public const string DbName = "db.name";
        public const string DbOperation = "db.operation";
        public const string NetPeerName = "net.peer.name";

        // Cosmos Db Specific
        public const string ClientId = "db.cosmosdb.client_id";
        public const string MachineId = "db.cosmosdb.machine_id";
        public const string UserAgent = "user_agent.original"; // Compliant with open telemetry conventions
        public const string ConnectionMode = "db.cosmosdb.connection_mode";
        public const string OperationType = "db.cosmosdb.operation_type";

        // Request/Response Specifics
        public const string ContainerName = "db.cosmosdb.container";
        public const string RequestContentLength = "db.cosmosdb.request_content_length_bytes";
        public const string ResponseContentLength = "db.cosmosdb.response_content_length_bytes";
        public const string StatusCode = "db.cosmosdb.status_code";
        public const string SubStatusCode = "db.cosmosdb.sub_status_code";
        public const string RequestCharge = "db.cosmosdb.request_charge";
        public const string Region = "db.cosmosdb.regions_contacted";
        public const string ItemCount = "db.cosmosdb.item_count";
        public const string ActivityId = "db.cosmosdb.activity_id";
        public const string CorrelatedActivityId = "db.cosmosdb.correlated_activity_id";

        // Exceptions
        public const string ExceptionType = "exception.type";
        public const string ExceptionMessage = "exception.message";
        public const string ExceptionStacktrace = "exception.stacktrace";
    }
}

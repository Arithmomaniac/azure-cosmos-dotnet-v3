﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using Microsoft.Data.Encryption.Cryptography;

    /// <summary>
    /// This class provides extension methods for <see cref="CosmosClient"/>.
    /// </summary>
    public static class EncryptionCosmosClientExtensions
    {
        /// <summary>
        /// Get Cosmos Client with Encryption support for performing operations using client-side encryption.
        /// </summary>
        /// <param name="cosmosClient">Regular Cosmos Client.</param>
        /// <param name="cosmosEncryptionKeyStoreProvider">CosmosEncryptionKeyStoreProvider, provider that allows interaction with the master keys.</param>
        /// <returns> CosmosClient to perform operations supporting client-side encryption / decryption.</returns>
        public static CosmosClient WithEncryption(
            this CosmosClient cosmosClient,
            CosmosEncryptionKeyStoreProvider cosmosEncryptionKeyStoreProvider)
        {
            if (cosmosEncryptionKeyStoreProvider == null)
            {
                throw new ArgumentNullException(nameof(cosmosEncryptionKeyStoreProvider));
            }

            if (cosmosClient == null)
            {
                throw new ArgumentNullException(nameof(cosmosClient));
            }

            // set the TTL for ProtectedDataEncryption at the Encryption CosmosClient Init so that we have a uniform expiry of the KeyStoreProvider and ProtectedDataEncryption cache items.
            if (cosmosEncryptionKeyStoreProvider.DataEncryptionKeyCacheTimeToLive.HasValue)
            {
                ProtectedDataEncryptionKey.TimeToLive = cosmosEncryptionKeyStoreProvider.DataEncryptionKeyCacheTimeToLive.Value;
            }
            else
            {
                // If null is passed to DataEncryptionKeyCacheTimeToLive it results in forever caching hence setting
                // arbitrarily large caching period. ProtectedDataEncryptionKey does not seem to handle TimeSpan.MaxValue.
                ProtectedDataEncryptionKey.TimeToLive = TimeSpan.FromDays(36500);
            }

            return new EncryptionCosmosClient(cosmosClient, cosmosEncryptionKeyStoreProvider);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Utils
{
    public class ClientInfo
    {
        public const string ClientInfoParamName = "client_info";
        public const string UniqueObjectIdentifierName = "uid";
        public const string UniqueTenantIdentifierName = "utid";

        [JsonPropertyName(UniqueObjectIdentifierName)]
        public string UniqueObjectIdentifier { get; set; } = null;

        [JsonPropertyName(UniqueTenantIdentifierName)]
        public string UniqueTenantIdentifier { get; set; } = null;

        public static ClientInfo CreateFromJson(string clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo))
            {
                throw new ArgumentNullException(nameof(clientInfo), "ClientInfo is null");
            }

            var bytes = Base64UrlHelpers.DecodeBytes(clientInfo);
            return bytes != null ? DeserializeFromJson(bytes) : null;
        }

        private static ClientInfo DeserializeFromJson(byte[] jsonByteArray)
        {
            if (jsonByteArray == null || jsonByteArray.Length == 0)
            {
                return default;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            return JsonSerializer.Deserialize<ClientInfo>(jsonByteArray, options);
        }
    }
}

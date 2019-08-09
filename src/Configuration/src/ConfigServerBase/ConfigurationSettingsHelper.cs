﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public static class ConfigurationSettingsHelper
    {
        private const string SPRING_APPLICATION_PREFIX = "spring:application";
        private const string VCAP_APPLICATION_PREFIX = "vcap:application";
        private const string VCAP_SERVICES_CONFIGSERVER_PREFIX = "vcap:services:p-config-server:0";
        private const string VCAP_SERVICES_CONFIGSERVER30_PREFIX = "vcap:services:p.config-server:0";

        public static void Initialize(string configPrefix, ConfigServerClientSettings settings, IConfiguration config)
        {
            if (configPrefix == null)
            {
                throw new ArgumentNullException(nameof(configPrefix));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var clientConfigsection = config.GetSection(configPrefix);

            settings.Name = GetApplicationName(configPrefix, config, settings.Name);
            settings.Environment = GetEnvironment(clientConfigsection, settings.Environment);
            settings.Label = GetLabel(clientConfigsection);
            settings.Username = GetUsername(clientConfigsection);
            settings.Password = GetPassword(clientConfigsection);
            settings.Uri = GetUri(clientConfigsection, settings.Uri);
            settings.Enabled = GetEnabled(clientConfigsection, settings.Enabled);
            settings.FailFast = GetFailFast(clientConfigsection, settings.FailFast);
            settings.ValidateCertificates = GetCertificateValidation(clientConfigsection, settings.ValidateCertificates);
            settings.RetryEnabled = GetRetryEnabled(clientConfigsection, settings.RetryEnabled);
            settings.RetryInitialInterval = GetRetryInitialInterval(clientConfigsection, settings.RetryInitialInterval);
            settings.RetryMaxInterval = GetRetryMaxInterval(clientConfigsection, settings.RetryMaxInterval);
            settings.RetryMultiplier = GetRetryMultiplier(clientConfigsection, settings.RetryMultiplier);
            settings.RetryAttempts = GetRetryMaxAttempts(clientConfigsection, settings.RetryAttempts);
            settings.Token = GetToken(clientConfigsection);
            settings.Timeout = GetTimeout(clientConfigsection, settings.Timeout);
            settings.AccessTokenUri = GetAccessTokenUri(configPrefix, config);
            settings.ClientId = GetClientId(configPrefix, config);
            settings.ClientSecret = GetClientSecret(configPrefix, config);
            settings.TokenRenewRate = GetTokenRenewRate(clientConfigsection);
            settings.DisableTokenRenewal = GetDisableTokenRenewal(clientConfigsection);
            settings.TokenTtl = GetTokenTtl(clientConfigsection);
            settings.DiscoveryEnabled = GetDiscoveryEnabled(clientConfigsection, settings.DiscoveryEnabled);
            settings.DiscoveryServiceId = GetDiscoveryServiceId(clientConfigsection, settings.DiscoveryServiceId);
            settings.HealthEnabled = GetHealthEnabled(clientConfigsection, settings.HealthEnabled);
            settings.HealthTimeToLive = GetHealthTimeToLive(clientConfigsection, settings.HealthTimeToLive);

            // Override Config server URI
            settings.Uri = GetCloudFoundryUri(configPrefix, config, settings.Uri);
        }

        private static bool GetHealthEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("health:enabled", def);
        }

        private static long GetHealthTimeToLive(IConfigurationSection clientConfigsection, long def)
        {
            return clientConfigsection.GetValue("health:timeToLive", def);
        }

        private static bool GetDiscoveryEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("discovery:enabled", def);
        }

        private static string GetDiscoveryServiceId(IConfigurationSection clientConfigsection, string def)
        {
            return clientConfigsection.GetValue("discovery:serviceId", def);
        }

        private static int GetRetryMaxAttempts(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("retry:maxAttempts", def);
        }

        private static double GetRetryMultiplier(IConfigurationSection clientConfigsection, double def)
        {
            return clientConfigsection.GetValue("retry:multiplier", def);
        }

        private static int GetRetryMaxInterval(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("retry:maxInterval", def);
        }

        private static int GetRetryInitialInterval(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("retry:initialInterval", def);
        }

        private static bool GetRetryEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("retry:enabled", def);
        }

        private static bool GetFailFast(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("failFast", def);
        }

        private static bool GetEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("enabled", def);
        }

        private static string GetToken(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("token");
        }

        private static int GetTimeout(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("timeout", def);
        }

        private static string GetUri(IConfigurationSection clientConfigsection, string def)
        {
            return clientConfigsection.GetValue("uri", def);
        }

        private static string GetPassword(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("password");
        }

        private static string GetUsername(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("username");
        }

        private static string GetLabel(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("label");
        }

        private static string GetEnvironment(IConfigurationSection section, string def)
        {
            return section.GetValue("env", string.IsNullOrEmpty(def) ? ConfigServerClientSettings.DEFAULT_ENVIRONMENT : def);
        }

        private static bool GetCertificateValidation(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("validateCertificates", def) && clientConfigsection.GetValue("validate_certificates", def);
        }

        private static int GetTokenRenewRate(IConfigurationSection configServerSection)
        {
            return configServerSection.GetValue("tokenRenewRate", ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_RENEW_RATE);
        }

        private static bool GetDisableTokenRenewal(IConfigurationSection configServerSection)
        {
            return configServerSection.GetValue("disableTokenRenewal", ConfigServerClientSettings.DEFAULT_DISABLE_TOKEN_RENEWAL);
        }

        private static int GetTokenTtl(IConfigurationSection configServerSection)
        {
            return configServerSection.GetValue("tokenTtl", ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_TTL);
        }

        private static string GetClientSecret(string configPrefix, IConfiguration config)
        {
           return GetSetting(
               "credentials:client_secret",
               config,
               ConfigServerClientSettings.DEFAULT_CLIENT_SECRET,
               VCAP_SERVICES_CONFIGSERVER_PREFIX,
               VCAP_SERVICES_CONFIGSERVER30_PREFIX,
               configPrefix);
        }

        private static string GetClientId(string configPrefix, IConfiguration config)
        {
            return GetSetting(
                "credentials:client_id",
                config,
                ConfigServerClientSettings.DEFAULT_CLIENT_ID,
                VCAP_SERVICES_CONFIGSERVER_PREFIX,
                VCAP_SERVICES_CONFIGSERVER30_PREFIX,
                configPrefix);
        }

        private static string GetAccessTokenUri(string configPrefix, IConfiguration config)
        {
            return GetSetting(
                "credentials:access_token_uri",
                config,
                ConfigServerClientSettings.DEFAULT_ACCESS_TOKEN_URI,
                VCAP_SERVICES_CONFIGSERVER_PREFIX,
                VCAP_SERVICES_CONFIGSERVER30_PREFIX,
                configPrefix);
        }

        private static string GetApplicationName(string configPrefix, IConfiguration config, string defName)
        {
            return GetSetting(
                "name",
                config,
                defName,
                configPrefix,
                SPRING_APPLICATION_PREFIX,
                VCAP_APPLICATION_PREFIX);
        }

        private static string GetCloudFoundryUri(string configPrefix, IConfiguration config, string def)
        {
            return GetSetting(
                "credentials:uri",
                config,
                def,
                configPrefix,
                VCAP_SERVICES_CONFIGSERVER_PREFIX,
                VCAP_SERVICES_CONFIGSERVER30_PREFIX);
        }

        /// <summary>
        /// Get setting from config searching the given configPrefix keys in order. Returns the first element with key.
        /// </summary>
        /// <param name="key">The key of the element to return.</param>
        /// <param name="config">IConfiguration to search through.</param>
        /// <param name="defaultValue">The default Value if no configuration is found.</param>
        /// <param name="configPrefixes">The prefixes to search for in given order.</param>
        /// <returns>Config value</returns>
        private static string GetSetting(string key, IConfiguration config, string defaultValue, params string[] configPrefixes)
        {
            foreach (var prefix in configPrefixes)
            {
                var section = config.GetSection(prefix);
                var result = section.GetValue<string>(key);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return defaultValue;
        }
    }
}

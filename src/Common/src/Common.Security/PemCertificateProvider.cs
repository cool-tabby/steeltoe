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
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Security
{
    public class PemCertificateProvider : ConfigurationProvider
    {
        private IConfigurationRoot _certFileProvider;
        private IConfigurationRoot _keyFileProvider;

        public PemCertificateProvider(IConfigurationRoot certFileProvider, IConfigurationRoot keyFileProvider)
        {
            _certFileProvider = certFileProvider;
            _keyFileProvider = keyFileProvider;
            _certFileProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
            _keyFileProvider.GetReloadToken().RegisterChangeCallback(NotifyKeyChanged, null);
        }

        public override void Load()
        {
            // for future use
        }

        public override void Set(string key, string value)
        {
            throw new InvalidOperationException();
        }

        public override bool TryGet(string key, out string value)
        {
            value = _certFileProvider[key];
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }

            value = _keyFileProvider[key];
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }

            return false;
        }

        private void NotifyCertChanged(object state)
        {
            OnReload();
            _certFileProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
        }

        private void NotifyKeyChanged(object state)
        {
            OnReload();
            _keyFileProvider.GetReloadToken().RegisterChangeCallback(NotifyKeyChanged, null);
        }
    }
}

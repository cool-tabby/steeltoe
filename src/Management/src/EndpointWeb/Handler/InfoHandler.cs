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

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class InfoHandler : ActuatorHandler<InfoEndpoint, Dictionary<string, object>>
    {
        public InfoHandler(InfoEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<InfoHandler> logger = null)
           : base(endpoint, securityServices, mgmtOptions, null, true, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public InfoHandler(InfoEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<InfoHandler> logger = null)
            : base(endpoint, securityServices, null, true, logger)
        {
        }
    }
}

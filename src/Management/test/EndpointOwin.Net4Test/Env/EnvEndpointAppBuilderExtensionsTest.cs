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
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Env.Test
{
    public class EnvEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseEnvActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseEnvActuator(config));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseEnvActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseEnvActuator(null));
            Assert.Equal("config", exception.ParamName);
        }
    }
}

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

using System;

namespace Steeltoe.Stream.Binding
{
    public abstract class AbstractBindingTargetFactory<T> : IBindingTargetFactory<T>
    {
        public bool CanCreate(Type type)
        {
            return type.IsAssignableFrom(typeof(T));
        }

        public abstract T CreateInput(string name);

        public abstract T CreateOutput(string name);

        object IBindingTargetFactory.CreateInput(string name)
        {
            return CreateInput(name);
        }

        object IBindingTargetFactory.CreateOutput(string name)
        {
            return CreateOutput(name);
        }
    }
}
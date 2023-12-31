﻿// Copyright (C) Microsoft Corporation. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

namespace Voxta.DesktopApp
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
            Resources["AdditionalArgs"] = "--enable-features=ThirdPartyStoragePartitioning,PartitionedCookies";

        }
    }
}
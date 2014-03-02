// Copyright © 2013 Şafak Gür. All rights reserved.
// Use of this source code is governed by the MIT License (MIT).

using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Dawn.SocketAwaitable - Unit Tests")]
[assembly: AssemblyDescription("Provides unit tests for Dawn.SocketAwaitable.")]
[assembly: AssemblyProduct("Dawn Framework")]
[assembly: AssemblyCopyright("MIT")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
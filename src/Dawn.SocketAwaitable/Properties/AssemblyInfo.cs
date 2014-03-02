// Copyright © 2013 Şafak Gür. All rights reserved.
// Use of this source code is governed by the MIT License (MIT).

using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Dawn.SocketAwaitable")]
[assembly: AssemblyCompany("https://github.com/safakgur/Dawn.SocketAwaitable")]
[assembly: AssemblyDescription("Provides utilities for asynchronous socket operations.")]
[assembly: AssemblyProduct("Dawn Framework")]
[assembly: AssemblyCopyright("MIT")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.1.1.0")]
[assembly: AssemblyFileVersion("1.1.1.0")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
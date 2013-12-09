// Copyright
// ----------------------------------------------------------------------------------------------------------
//  <copyright file="AssemblyInfo.cs" company="https://github.com/safakgur/Dawn.SocketAwaitable">
//      MIT
//  </copyright>
//  <license>
//      This source code is subject to terms and conditions of The MIT License (MIT).
//      A copy of the license can be found in the License.txt file at the root of this distribution.
//  </license>
//  <summary>
//      Contains the attributes that provide information about the assembly.
//  </summary>
// ----------------------------------------------------------------------------------------------------------

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
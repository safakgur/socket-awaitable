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
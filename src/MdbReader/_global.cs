// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.Text;
global using BitArray = System.Collections.BitArray;
global using DebuggerDisplayAttribute  = System.Diagnostics.DebuggerDisplayAttribute ;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("MMKiwi.MdbReader.Tests")]
[assembly: InternalsVisibleTo("MMKiwi.MdbReader.WindowsTests")]
[assembly: InternalsVisibleTo("MMKiwi.MdbReader.JsonModel")]
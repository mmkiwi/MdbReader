# MdbTools in C#

Currently pre-pre alpha, can read table definition and row values. Loosely based on MdbTools
(https://github.com/mdbtools/mdbtools) and deeply indebted to their work documenting the mdb format
(https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md)

## Current Status

The library is currently read-only and cannot modify databases at all. It has support for both Jet3 and Jet4-style
databases. (Any version of access later than 1995). It has not been thoroughly tested and should not be used in
production code. It features heave use of Span&lt;byte&gt; to ensure that there are no buffer overruns, but it may crash on
some databases.

Indices are not currently supported, but may be at a future time. Indices are not required to read the data in the table.
Rows can be enumerated one-by-one. There is no querying or indexing supported besides the standard extensions to IEnumerable
in .NET.

For an example of the API, see the test/MdbCreateJson folder.

This library is developed for .NET 7, but had polyfills to run (with slightly worse memory usage and performance) on 
earlier versions through .NET standard 2.1. The library is written entirely in C# and does not have any specific OS
requirements. It has been coded so that it should perform correctly on big-endian systems, but this has not yet been
tested.

## Roadmap

* Improve unit testing
* Support for writing
* Test on big-endian system
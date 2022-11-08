# MdbTools in C#

Currently pre-pre alpha, can only read a table definition. Loosely based on MdbTools
(https://github.com/mdbtools/mdbtools) and deeply indebted to their work documenting the mdb format
(https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md)

## Current Status

The library is currently only functional for Jet3 databases. It is currently read-only and cannot
modify databases at all.

For an example of the API, see the test/MdbCreateJson folder.

This library is developed for .NET 7, but had polyfills to run (with slightly worse memory usage and performance) on 
earlier versions through .NET standard 2.1. The library is written entirely in C# and does not have any specific OS
requirements. It has been coded so that it should perform correctly on big-endian systems, but this has not yet been
tested.

## Roadmap

* Support Jet4+
* Improve unit testing
* Support for writing
* Support for .NET standard 2.1
* Test on big-endian system
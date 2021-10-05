# CodeIndex Project

This project started out as a way to entertain myself mid flight from Seattle to LA.

The highlevel goal(s) are to:

- Explore search and indexing concepts.
- Explore disk IO, caching, and disk access optimization concepts.
- Create an instantaneous command line tool for searching through code.
- Integrate with Visual Studio in a way that improves performance and capabilities of existing search features.

The repo currently has:

- Hack and slash prototype (read: terrible quality) prototype code indexer and index reader.
- A hacky, interesting framework for building large, single-file caches in the form of lists or dictionaries, with built in paging/page-caching.
- Server app that accepts index and search requests via JSON RPC formatted as MessagePack.
- Proof of concept Visual Studio extension that makes requests to the server app.

Next steps are to solidify this skeletal app into something useful.

# v2 RoadMap

For v2, I am working on making a couple of improvements in pursuit of:

- Incremental indexing
- Better scalability of the ingestion portion by making the virtualized list writable and doing index via paging, like we do the lookup.
- Test driven development.
- Substring search via a change in the structure of the index.

## Proposed architecture

### Disk manager

- A C# API around a memory mapped file on disk.
- Keeps track of active and inactive pages in the file.
- Maintains a cache of active pages.
- Cache should optimize lifetime of each page based on memory constraints and number of accesses.

### B-Tree

- New portion of the implementation that expands on the internals of the paging list.
- B-tree breaks up contiguous paging list into small, fixed length units, to enable the paging list to be modified after it is created.
- Both paging list and paging dictionary will now be built on the B-tree (exposed as a list) and should support rebalancing.

### Search index

- Words map: PagingList2D<Entry>, where offset is the value of a C# character and the result is a list of entries, where each entry is a (word, fileIndex, frequency) triple.
- Files list: PagingList<VarChar>, where the index is the fileIndex, and the value at that index is the serialized file name string.

### Search procedure:

- Tokenize search string, using the same tokenization algorithm as the ingestion job. Emphasis should be put on coming up with a reasonable set of short tokens
  separate on punctuation, whitespace, etc.

- Lookup the set of tokens containing each char. Union
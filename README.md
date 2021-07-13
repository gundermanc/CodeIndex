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
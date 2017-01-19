# XMPlay Sharp Scrobbler #

A Last.fm scrobbling plugin for the XMPlay audio player.

This project started just after the debacle of the new Last.fm website (summer of 2015), which, among other things, broke the xmp-scrobbler plugin I was using at the time.
The xmp-scrobbler plugin, still in beta, unmaintained since 2010, its source code almost lost, was using a multiple time obsolete version of the scrobbling API.
Someone at Last.fm somehow thought it would be a good idea to change the behaviour of this old API, and since then, xmp-scrobbler crashes upon start...

After 10 days of unbearable scrobbling withdrawal, hoping it would get fixed, nothing changed.
So I started working on my own scrobbler form scratch.

This is the result.

## Requirements ##

- [Visual C++ Redistributable for Visual Studio 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48145) (Only the x86 version is required)
- [.Net 4.6](http://www.microsoft.com/en-us/download/details.aspx?id=48137) (Link to the offline installer)

*Warning*: the .Net 4.6 dependency means this plugin won't work on XP or lower.

This plugin was developed with the XMPlay 3.8 API. It is likely to work with an older version, but I make no guarantee.

## Features ##

- Uses the up to date, still maintained by Last.fm, and unlikely to break scrobbling web API
- Does not require you to enter your username or password in clear text in the configuration (OAuth support)
- Correctly updates the Now Playing track in real time on your Last.fm profile
- As per the 2.0 API documentation, seeking in the track is allowed
- Can store an infinite amount of tracks waiting to be scrobbled in case Last.fm or your connection is down
- Comprehensive logging
- Actually works!

## License ##

This plugin is open source and released under the MIT license.

## Notes ##

Yes, I'm using .Net.
Most of the useful stuff in this plugin is actually written in C#.
I don't think C or C++ are really appropriate for high level asynchronous code querying a REST web API, so I created a native C++ wrapper around a C# library handling most of the web related stuff.
There remains a relatively small part in pure C++ handling the track playing duration detection and the tag extraction from XMPLay.
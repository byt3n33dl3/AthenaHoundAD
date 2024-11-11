<p align="center">
  <img src="agent_icons/athena_old.svg">
</p>

# AthenaHoundAD
AthenaHoundAD is a fully-featured cross-platform agent designed using the crossplatform version of .NET (not to be confused with .Net Framework). AthenaHoundAD is designed for Mythic 3.0 and newer.

## Workflows
[![Agent Builds](https://github.com/MythicAgents/Athena/actions/workflows/dotnet-desktop.yml/badge.svg?branch=main)](https://github.com/MythicAgents/Athena/actions/workflows/dotnet-desktop.yml)

[![Build and push container images](https://github.com/MythicAgents/Athena/actions/workflows/docker.yml/badge.svg?branch=main)](https://github.com/MythicAgents/Athena/actions/workflows/docker.yml)

## Features
- Crossplatform
  - Windows
  - Linux
  - OSX
  - Potentially More!
- SOCKS5 Support
- Reverse Port Forwarding
- P2P Agent support
	- SMB
	- More coming soon
- Reflective loading of Assemblies
- Modular loading of commands
- Easy plugin development
- Easy development of new communication methods
- BOF Support

## Installation

1.) Install Mythic from [here](https://github.com/byt3n33dl3/AthenaHoundAD)

2.) From the Mythic install directory run the following command:

`./AthenaHoundAD install github https://github.com/byt3n33dl3/AthenaHoundAD`

# Credits
[@byt3n33dl3](https://twitter.com/byt3n33dl3) - Creator of the AthenaHoundAD

[@its_a_feature_](https://twitter.com/its_a_feature_) - Creator of the Mythic framework

[@0okamiseishin](https://twitter.com/0okamiseishin) - For creating the Athena logo

[@djhohnstein](https://twitter.com/djhohnstein) - For crypto code, and advice regarding development

[@tr41nwr3ck](https://twitter.com/Tr41nwr3ck48) - For plugin Development & Testing

## Known Issues
- Athena cannot be converted to shellcode
  - Due to the nature of self-contained .NET executables, Athena is currently unable to be converted to shellcode with tool such as donut
- Large Binary Sizes
  - Athena binaries default to being "self-contained", this essentially means the entire .NET runtime is included in the binary leading to larger sizes. If you need smaller binaries, experiment with the `trimmed`, and `compressed` options.
- Athena doesn't work with <insert common .NET executable here>
  - Athena is built using the latest version of .NET which is fundamentally different from the .NET Framework, which a majority of offensive security tools use. Any .NET Framework binaries will need to be converted to .NET 7 before they can be used with `execute` assembly alternatively, you can use `inject` assembly to use `donut` to convert it to shellcode and inject into a sacrificial process.

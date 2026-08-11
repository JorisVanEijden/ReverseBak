# Project

This project contains tooling used to investigate the old Betrayal at Krondor MS-DOS game.

## About

This is a project focused on reverse engineering the classic MS-DOS game Betrayal at Krondor. The goal is to understand the game's data formats and mechanics to eventually create a highly moddable version in Unity.

## Project Status

The project currently consists of two main components:

### ResourceExtractor
- Converts original game data files to .NET objects
- Exports data as JSON/CSV for analysis
- Development tool with command line interface

### BetrayalAtKrondor
- Works with [Spice86](https://github.com/OpenRakis/Spice86) DOS emulator
- Provides C# overrides for original game functions
- Enables debugging, logging and investigation of game behavior
- Replaces low-level DOS file handling for better control

## Getting Started

### Prerequisites
- .NET 9 SDK
- Clone the repository
- IDE (Rider recommended)

### Setup & Running
See the [GitHub repository](https://github.com/JorisVanEijden/ReverseBak) for detailed setup instructions.

## Project Goals
1. Document and understand Betrayal at Krondor's data formats
2. Create tools to extract and analyze game assets
3. Eventually rebuild the game in Unity with modding support

## Contributing
The project is currently in research phase. Feel free to open issues or discussions on GitHub.

## Tools Used
- [DOSBox-X](https://dosbox-x.com/)
- [Ghidra](https://ghidra-sre.org/)
- [IDA Freeware 5.0](https://www.scummvm.org/news/20180331/)
- [HxD](https://mh-nexus.de/en/hxd/)

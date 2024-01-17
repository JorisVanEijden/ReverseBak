# ReverseBak ReadMe

This project contains tooling used to investigate the old Betrayal at Krondor MS-DOS game.

It contains no BaK source code or data files.

Once enough information has been gathered, a new repo will be created to build a highly moddable version of the game in Unity. 

Other tooling used:
- [DOSBox-X](https://dosbox-x.com/)
- [Ghidra](https://ghidra-sre.org/)
- [IDA Freeware Version 5.0](https://www.scummvm.org/news/20180331/)
- [HxD](https://mh-nexus.de/en/hxd/)


### Prerequisites:
- .NET 8 SDK (https://dotnet.microsoft.com/en-us/download)
- Clone the repository locally
- Open the BetrayalAtKrondor.sln in your IDE of choice (I use [Rider](https://www.jetbrains.com/rider/))

## ResourceExtractor

This aims to convert original game data files to .NET objects, which can be saved as json or csv.

This is a development tool with no GUI, no documentation and no help.

### Setup
- Uncomment/add/change anything you like in the Program.cs

### Run
- I use the built-in run/debug option in Rider to run the project.
- You can also build it, find the executable and run it from the commandline or a script.

## BetrayalAtKrondor

### Project 
This is supposed to work in conjunction with Spice86(https://github.com/OpenRakis/Spice86)

Spice86 is an MSDOS emulator like DosBox. But this one allows you to replace functions from the original program with C# code.

This project contains such overrides. They are used to replace existing functionalty, to add debugging, logging and perform other investigative functions.
For example, all the low-level DOS file handling has been replaced by C# code. This allows us to fully control and snoop on all file operations.

### Setup

For development purposes I have referenced my local Spice86 repository as project references. This will fail on your machine.
In order to "fix" this you'll need to clone the [https://github.com/JorisVanEijden/Spice86] repository to the location indicated in the BetrayalAtKrondor.sln file (or change that file to point at the Spice86 locations).

### Run
Then run the program from your IDE, or build it to run it standalone.

Use these program arguments: `--Ems --Exe "C:\Games\Betrayal at Krondor\KRONDOR.EXE" --UseCodeOverride=true` but replace the exe path with your KRONDOR.EXE location.

This will start the emulator and output logging.

For more information about Spice86, command line arguments and overrides, see https://github.com/OpenRakis/Spice86

# RBMobileUnpacker
An unpacker/repacker for various mobile Rock Band titles. There isn't a whole lot of info currently available for the handheld/mobile RB games, and I wrote this tool a while ago and forgot about it, so I thought I would publish it.

The Rock Band DS games have a massive "Glob.bin" file that contain most files in the game, stored without a name table. There is a "GlobFile.txt" that sits inside the Glob.bin that contains the names of all the files, including itself. The tool is capable of unpacking and repacking Glob.bin files, and properly naming files, so the files can be modded.

As for audio, LEGO Rock Band DS stores the stems inside the Glob itself. RB3 DS uses a CRI AWB file. The AWB format is well understood and I believe repacking/unpacking tools exist, but there is currently no support in this tool.

Rock Band: Unplugged for PSP remains an enigma. Done a lot of looking but still cannot figure it's STR(eam) format out.

Rock Band for iOS is currently unsupported.

## Supported Games
* Rock Band 3 DS (Glob.bin only)
* LEGO Rock Band DS

## Support Planned
* Rock Band: Unplugged
* Rock Band iOS

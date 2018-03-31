# ovlinject
---
This is a _command line_ utility written in C# to inject compiled overlay (actor) files binaries into a supported Zelda 64 ROM.
Right now the only supported rom is the Ocarina of Time Master Quest Debug ROM. The usage is as follows:

```
Usage: [PROGRAM] -i/-infile [-s/-vram] [-a/-actor] [-r/-rom] [-o/-offset] [-v/-verbose]
       [-i]: Infile. This is the overlay file to process. This argument is not optional.
       [-s]: (vRAM) start. This is the overlay's virtual RAM start; default is 0x80800000.
       [-a]: Actor Number. This is the overlay entry to overwrite; default is 0x0001.
       [-r]: Ocarina of Time Master Quest Debug ROM to inject into.
             If no ROM is provided, the overlay entry will still be generated.
       [-o]: ROM Injection Offset. This is where the overlay file will be written; default is 0x035D0000.
       [-v]: Write verbose overlay information to the console.
Example: ovlinject.exe -i En_Sa.zovl -s 0x80AF5560 -a 0x0146 -r ZELOOTMA.z64 -o 0x035D0000 -v
CrookedPoe - March 2018
```

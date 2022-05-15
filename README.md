# Midi Style Explorer
A windows tool for opening, playing, and manipulating midi and Yamaha style files.

Requires VS2019 and .NET5.

Uses [MidiLib](https://github.com/cepthomas/MidiLib/blob/main/README.md).

# Usage
- Opens style files and plays the individual sections.
- Export style files as their component parts.
- Export current selection(s) and channel(s) to a new midi file. Useful for snipping style patterns.
- Click on the settings icon to edit your options.
- Some midi files with single instruments are sloppy with channel numbers so there are a couple of options for simple remapping.
- In the log view: C for clear, W for word wrap toggle.

# Notes
- Since midi files and NAudio use 1-based channel numbers, so does this application, except when used as an array index.
- Because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.
- If midi file type is `1`, all tracks are combined. Because.
- Tons of styles and info at https://psrtutorial.com/.

# Third Party
This application uses these FOSS components:
- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).

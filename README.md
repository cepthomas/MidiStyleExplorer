# MidiStyleExplorer
A windows tool for playing audio and midi file clips. This is intended to be used for auditioning parts for use in compositions created in a real DAW.
To that end, and because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.

# Usage
- If midi file type is one, all tracks are combined. Because.
- Opens Yamaha style (.sty) files and plays the individual sections.
- Export current selection to a new midi file. Useful for snipping style patterns.
- Click on the settings icon to edit your options.
- Some midi files with single instruments are sloppy with channel numbers so there are a couple of options for simple remapping.
- In the log view: C for clear, W for word wrap toggle.


# Third Party
This application uses these FOSS components:
- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).

# Simple packages with the MD Format 
One program always seems to begat more programs. A database implies a schema editor, a Dump program, a program to diagnose and fix broken database files, and more. A simple IOT-style Bluetooth Proximity program to unlock a lock or turn on a light when a known Bluetooth device is present almost requires a little auxilary program to learn Bluetooth addresses and another one to wipe clean and reset the lock program.

For the professional programmer, handling these programs is just something we do -- we make sure they're part of the build system, and make sure that all the source is neatly checked into a source control system system. Somewhere the entire system is described so the end user can figure out which program to use and when.

The Best Calculator BASIC system is entirely designed for the non-professional programmer. The Best Calculator is a Windows and Windows Phone calculator app that can be programmed in BASIC. The BASIC has been extended from the old microcomputer BASICs: line numbers are not needed, it's easy to write proper functions (removing the need for GOSUB), and it supports modern features like high resolution graphics and Unicode.

In the BC BASIC system, users are encouraged to group their programs into *packages*: each package has a name, description and a set of named programs, each of which also has a description. (Actually, it's not just encouraged; all programs in BC BASIC are automatically part of a package.)

The first version of BC BASIC has each package created as a JSON file. These packages were simple and robust, but had two problems. Firstly, it was hard to convert the package into a publishable file. The BC BASIC documentation includes several sample packages to show off good programming practices. The JSON had to be converted into a format that could be published. The system includes exporters for HTML and RTF (Rich Text Format) files, but neither provides good-looking results when imported into the overall documentation. Secondly, sometimes an expert programmer prefers to hand-edit a package file inside a more powerful external editor (like Notepad++) instead of making changes inside the less capable built-in editor. Editing JSON files that include embedded quotes for the BASIC strings was cumbersome and error-prone.

The second version of BC BASIC switched to using MD (Markdown) files as the package format. The package name is set by the first double-## header line; the text after that is the package description up to the first tripple-### line which is the name of the first program.   From there, the text is all the program description until the first triple-\`\`\` BASIC backtick line which start the program proper, all the way up to the concluding `\`\` line. 

A very simple example of one of the package files is below. At the end are the complete rules for reading MD files as a package of programs.

```MD
## Bluetooth Proximity Examples
A set of programs to demonstrate how to build a Bluetooth 
Proximity detector. When a known Bluetooth device comes 
in range, the Proximity Detector will perform an action (specifically, 
will turn on a light). The Bluetooth Proximity program is the 
full progam; the Watcher program lets you learn the Bluetooth 
Address of a device.
### Proximity Detector
The Proximity Detector is a full-fledged program for you to
modify. It will look for a set of Bluetooth devices based on 
a universal memory value (which can be set by the Editor)
```BASIC
REM Proximity Detector Program
CLS BLUE
PRINT "Proximity Detector"
...
` ` `
```
(Sorry, MD doesn't have any clean way to embed the triple-\`\`\` inside of a code block; instead I had to add spaces between the backquotes).

The actual parsing and writing code, of course, is more precise. Programs, for example, can potentially include post-code text which is preserved. In a normal package file, the only thing that comes after the concluding triple-\`\`\` backtick is a triple-## line to define a new program. The parser will carefully read any extra data there, and the writer code will carefully preserve it.Similarly, text before the first single-# header is a preable comment for the package and is carefully read and preserved.

#### The full rules for parsing a MD formatted package are:

Each line is one of exactly 7 types: Package Name, Package Description, Package Predescription, Program Name, Program Description, Program Code and Program PostCode

There are 5 states: Reading Package Predescription (the start state), Reading Package Description, Reading Program Description, Reading Program Code, and Reading Program PostCode. The are fewer states than line types because two of he line types (Package Name and Program Name) are alqways exactly one line line.

##### When in state **Reading Package Predescription**
1. When a line starts with "## ", it's a Package Name. The name doesn't include the "## " or any other starting or ending whitespace (the rules of which are the ones for .NET string.Trim()). At this point the state is set to Reading Package Description
2. Else the line is added to the Package Predescription.

##### When in state **Reading Package Description**
1. When a line starts with "### ", it's a Program Name. The name doesn't include the "### " or any other starting or ending whitespace (the rules of which are the ones for .NET string.Trim()). At this point the state is set to Reading Program Description
2. Else the line is added to the Package Description.

##### When in state **Reading Program Description**
1. When a line starts with  "\`\`\`", it's the start of the Program Code. At this point the state is set to Reading Program Code
2. When a line starts with "\*\*Default Key\*\*:", the rest of the line (after trimming the start and end whitespace) is the super-short name of the program that should be put on the user-definable keys.
2. Else the line is added to the Program Description.

##### When in state **Reading Program Code**
1. When a line starts with "\`\`\`", it's the end of the Program Code. At this point the state is set to Reading Program PostCode
2. Else the line is added to must be part of the Program Description.

##### When in state **Reading Program PostCode**
1. When a line starts with "### ", it's another Program. Just like in the Reading Package Description, the Program Name is the value without starting or ending whitespace and the stsate is set to Reading Program Description.
2. Else the line is added to must be part of the Program PostCode.

Obviously a package can contains multiple programs. Each Program Name line which is read indicates that a new program is to be set up, and that the following Program Description, Program Code, and Program PostDescription are associated with that program. There can be multiple program with the same name (the name isn't an ID, and no ID is present in the package.)

In addition, programs can contain a little bit more data than just the name, description and code. Programs can include ProgramKey values: these are strings which will be used when the program is bound to a programmable key. When the user puts Best Calculator into calculator mode, there are four programmable keys. When pressed, a programmable key will run the associated program.

The ProgrammableKey is embedded inside the program description. If a line in the program description starts with \*\*Default Key\*\*: then the line is setting the programmable key value. Then can be zero, one, or more Default Key lines, but only the last line is used.

The end result are package description files that are simple to understand, simple to edit with a text editor, and which can be quickly and robustly parsed. The MarkDown format, somewhat to my surprise, works quite well when put to this novel use.
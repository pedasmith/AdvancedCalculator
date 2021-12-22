# Using Markdown for writing programs

Markdown is a great "wrapper" format for writing program documentation. Not only can you write rich documentation, but your code is embedded as well.

BC BASIC uses Markdown as the primary way to store programs. There are some unique features in BC BASIC that makes Markdown a particularly good choice for a container format

Firstly, BC BASIC programs are usually written as a bunch of programs. There are a bunch of sample statistics programs; these are grouped into a single *package* of programs. Simlarly, when people write one little program in a domain, chance are there are related programs that ought to be grouped together.

Secondly, BC BASIC is designed for short programs. All of the fancy tricks that are needed by full-fledged IDEs designed for full-time programmers working on giant programs are not needed for the kind of lightweight, retro-style programs that are BC BASIC's design point.

Lastly, BC BASIC is designed to be shared. If you've got a new game, or a small utility, or have a Bluetooth control program, the next step ought to be to show off your program.  BC BASIC encourages per-package and per-program documentation; that documentation is a first-class citizen of the IDE instead of something that needs to be bolted on afterwards.

## Markdown for Languages

The parser for Markdown for Languages is simple
1. The .md files are parsed line-by-line using C# ReadLines
2. Everything before the first H2 (## ) header line is a *package pre comment*
3. The contents of the first H2 (## ) line, after removing the first # and space, is the *package name*
4. The next lines, up to the first H3  (### ) lines is the *package comment*
5. The first H3 (### ) line is the *program name*
6. Everything up to the first program block line (\`\`\`) is the *program comment*.  The language is always ignored (e.g., \`\`\`BASIC) and will be replaced when written out with \`\`\`BASIC
7. Everything up to the next program block lin (\`\`\`) is the *program text* and is expected to be BC BASIC format.
8. Everything up to the next H3 (### ) line is the *program end comment*
9. The next H3 (###) line is another program; loop to step 5!

The IDE gives an easy way to set the *package name*, the *package comment*, the *program name*, the *program comment* and the *program text*.  The BC BASIC IDE does not provide a way to set or view the*package pre comment* or the *program post comment*, although these will be preserved.  The package name and program name are always trimmed (the leading # or ## is removed and the resulting string is trimmed). 

There is a subtle extension on the definition of the program comment (line 6, everything from H3 (### ) to back-ticks (\`\`\`).  Any line starts with \*\*Default Key\*\*: is parsed as the key-name for the program. The key-name is the name of the key that the program is bound to.

There's also a subtle part about writing out a package.  The package pre comment isn't allowed to have an H2 line (## ) embedded; if there is one, the ## is escaped by prepending a backslash (\\) to the line. Similarly, the package comment isn't allowed to have an embedded H3 (###) and these will be escaped, the program comment isn't allowed to have an embedded back-tick line (each back-tick will be escaped, so \`\`\` becomes \\\`\\\`\\\`) and the program post-command isn't allowed to have an embedded program name (H3).  Note that the H2 and H3 lines are only escaped at the start of a line, where the "start" means "first nopn-whitespace char"




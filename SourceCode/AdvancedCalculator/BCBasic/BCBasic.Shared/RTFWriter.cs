using System;
using System.Text;

public class HtmlWriter
{
    public static string LibraryToHtml(BCBasic.BCLibrary library)
    {
        var content = "";
        foreach (var package in library.Packages)
        {
            content += ToHtmlFragment(package);
        }

        var Retval = WrapHtml(content);
        return Retval;
    }
    public static string PackageToHtml(BCBasic.BCPackage package)
    {
        var content = "";
        content += ToHtmlFragment(package);

        var Retval = WrapHtml(content);
        return Retval;
    }
    private static string WrapHtml(string htmlcontents)
    {
        var pre = @"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<style type='text/css'>
.CODE {
    background-color : #e0e0e0;
    white-space: pre-wrap;
    margin : 10px 50px;
    padding : 5px 5px;
    max-width : 100ch;
	font-family: 'Lucida Sans Unicode',monoface;
	font-size: 9.5pt;
}
</style>
</head>
<body>
";
        return pre + htmlcontents + "</body>";
    }
    private static string ToHtmlFragment(BCBasic.BCPackage package)
    {
        // Package name: \pard\sa200\sl276\slmult1\b\f0\fs24\lang9 PACKAGE1NAME\b0\fs22\par
        var name = @"<h2 class='PACKAGE_HEADER'>PACKAGE1NAME</h2>" + "\n";
        name = name.Replace("PACKAGE1NAME", HtmlEscape(package.Name));

        // Package description: Program11Description\par
        var description = @"<div class='PACKAGE_DESCRIPTION'>Package1Description</div>" + "\n";
        description = description.Replace("Package1Description", HtmlEscape(package.Description));

        var programs = "";
        foreach (var program in package.Programs)
        {
            programs += ToHtmlFragment(program);
        }

        var Retval = $"{name}\n{description}\n{programs}\n";
        return Retval;
    }

    private static string ToHtmlFragment(BCBasic.BCProgram program)
    {
        var name = @"<h3 class='PROGRAM_HEADER'>PROGRAM11NAME</h3>";
        name = name.Replace("PROGRAM11NAME", HtmlEscape(program.Name));

        var description = @"<div class='PROGRAM_DESCRIPTION'>Program11Description</div><br />" + "\n";
        description = description.Replace("Program11Description", HtmlEscape(program.Description));

        var code = @"<pre><div class='CODE'>";
        var lines = "";
        foreach (var line in program.CodeAsLines())
        {
            if (lines != "") lines += "\n";
            lines += HtmlEscape(line);
        }
        code += lines;
        code += @"</div></pre>" + "\n";

        var Retval = $"{name}\n{description}\n{code}\n";
        return Retval;
    }

    private static string HtmlEscape(string str)
    {
        var sb = new StringBuilder();
        foreach (var ch in str)
        {
            switch (ch)
            {
                case '<': sb.Append("&lt;"); break;
                case '&': sb.Append("&amp;"); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}


public class MdWriter
{
    public static string LibraryToMd(BCBasic.BCLibrary library)
    {
        var content = "";
        foreach (var package in library.Packages)
        {
            content += ToMdFragment(package);
        }

        var Retval = WrapMd(content);
        return Retval;
    }
    public static string PackageToMd(BCBasic.BCPackage package)
    {
        var content = "";
        content += ToMdFragment(package);

        var Retval = WrapMd(content);
        return Retval;
    }
    private static string WrapMd(string htmlcontents)
    {
        return htmlcontents;
    }
    private static string ToMdFragment(BCBasic.BCPackage package)
    {
        var prepackage = $"{MdEscape(package.PackagePreComment)}";
        if (prepackage != "") prepackage = prepackage.TrimEnd() + "\n";

        var name = $"## {MdEscape(package.Name)}\n";

        // Package description: Program11Description\par
        var description = $"{MdEscape(package.Description).TrimEnd()}\n";

        var programs = "";
        foreach (var program in package.Programs)
        {
            programs += ToMdFragment(program);
        }

        var Retval = $"{prepackage}{name}{description}{programs}";
        return Retval;
    }

    private static string ToMdFragment(BCBasic.BCProgram program)
    {
        var name = $"### {MdEscape(program.Name)}\n";

        var description = MdEscape(program.Description).TrimEnd() + "\n";

        var key = $"**Default Key**: {MdEscape(program.KeyName)}\n";
        if (program.KeyName == "") key = "";

        var code = @"```BASIC"+ "\n";
        var lines = "";
        foreach (var line in program.CodeAsLines())
        {
            lines += MdEscape(line) + "\n";
        }
        code += lines.TrimEnd();
        code += "\n```\n";
        var postprogram = program.ProgramPostComment != "" ? MdEscape(program.ProgramPostComment).TrimEnd()+"\n" : "";


        var Retval = $"{name}{description}{key}{code}{postprogram}";
        return Retval;
    }

    private static string MdEscape(string str)
    {
        var sb = new StringBuilder();
        foreach (var ch in str)
        {
            switch (ch)
            {
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}


public class RTFWriter
{
    public static string LibraryToRtf(BCBasic.BCLibrary library)
    {
        var content = "";
        foreach (var package in library.Packages)
        {
            content += ToRtfFragment(package);
        }

        var Retval = WrapRtf(content);
        return Retval;
    }
    public static string PackageToRtf(BCBasic.BCPackage package)
    {
        var content = "";
        content += ToRtfFragment(package);

        var Retval = WrapRtf(content);
        return Retval;
    }
    private static string WrapRtf (string rfccontents)
    {
        // {\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Calibri;}{\f1\fnil\fcharset0 Lucida Console;}}
        // {\*\generator Riched20 10.0.14393}\viewkind4\uc1
        // CONTENT
        // }
        var Retval = @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Calibri;}{\f1\fnil\fcharset0 Lucida Console;}}"
            + "\n" + @"{\*\generator Riched20 10.0.14393}\viewkind4\uc1"
            + "\n" + rfccontents
            + "\n" + @"}"
            + "\n"
            ;
        return Retval;
    }
    private static string ToRtfFragment (BCBasic.BCPackage package)
    {
        // Package name: \pard\sa200\sl276\slmult1\b\f0\fs24\lang9 PACKAGE1NAME\b0\fs22\par
        var name = @"\pard\sa200\sl276\slmult1\b\f0\fs24\lang9 PACKAGE1NAME\b0\fs22\par";
        name = name.Replace("PACKAGE1NAME", RtfEscape(package.Name));

        // Package description: Program11Description\par
        var description = @"Package1Description\par";
        description = description.Replace("Package1Description", RtfEscape(package.Description));

        var programs = "";
        foreach (var program in package.Programs)
        {
            programs += ToRtfFragment(program);
        }

        var Retval = $"{name}\n{description}\n{programs}\n";
        return Retval;
    }

    private static string ToRtfFragment(BCBasic.BCProgram program)
    {
        // Program name: \pard\sa200\sl276\slmult1\b\fs22 PROGRAM11NAME\b0\par
        var name = @"\pard\sa200\sl276\slmult1\b\fs22 PROGRAM11NAME\b0\par";
        name = name.Replace("PROGRAM11NAME", RtfEscape (program.Name));

        // Program description: Program11Description\par
        var description = @"Program11Description\par" + "\n";
        description = description.Replace("Program11Description", RtfEscape(program.Description));

        // Program code: \pard\li360\sa200\sl276\slmult1\fs20 Program12ProgramLine1\line Program12ProgramLine2\line Program12ProgramLine3\line Program12ProgramLine4\par
        var code = @"\pard\li360\sa200\sl276\slmult1\fs20 ";
        var lines = "";
        foreach (var line in program.CodeAsLines())
        {
            if (lines != "") lines += @"\line ";
            lines += RtfEscape (line);
        }
        code += lines;
        code += @"\par";

        var Retval = $"{name}\n{description}\n{code}\n";
        return Retval;
    }

    private static string RtfEscape(string str)
    {
        str = str.Replace(@"\", @"\\"); // double each backslash to escape it.
        var sb = new StringBuilder();
        foreach (var ch in str)
        {
            int ival = (int)ch;
            if (ival < 128) sb.Append(ch);
            else
            {
                // Thanks to http://stackoverflow.com/questions/1368020/how-to-output-unicode-string-to-rtf-using-c
                var unicode = $"\\u{Convert.ToUInt32(ch)}?";
                sb.Append(unicode);
            }
        }
        return sb.ToString();
    }
}

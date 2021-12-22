# Gets the version from the appxmanifest file and the current date
# Generates a version string
# Updates the image with the new value.


$endfile = "Best Calculator and BC BASIC.docx"
$endzip = "myresult.zip"
$startfile = "startdocument.zip"
$tempdir = "mydoc"


# Get the version from the appxmanifest file
$currPath=get-location
$path= "$currPath\start.appxmanifest"
$oXMLDocument=New-Object System.XML.XMLDocument  
$oXMLDocument.Load($path)
$version=$oXMLDocument.Package.Identity.Version.split(".")
$v1=$version[0]
$v2=$version[1]
$appVersion="$v1.$v2"

$date=get-Date
$month=(Get-Culture).DateTimeFormat.GetMonthName($date.Month)
$year=$date.Year	

$versionText = "Version $appVersion, $month $year"
write-host $versionText
magick "Cover-no-version.png" -fill black -pointsize 30 -annotate +460+1200 $versionText "cover_$appVersion.png"

copy cover_$appVersion.png image2.png



if (test-path $endzip) { remove-item $endzip }
if (test-path $endfile) { remove-item $endfile }

#
# Update the (unzipped) .DOCX file
#
copy "image2.png" "$tempdir\word\media"
#
# Replace the version string in the manual
#
$currPath=get-location
$path= "$currPath\mydoc\word\document.xml"
$txt = [Io.File]::ReadAllText($path) 
# Unicode Pocket Calculator 🖩
$lookFor=[char][UInt16]0xd83d+[char][UInt16]0xdda9
$start = $txt.IndexOf($lookFor)
$end = $txt.IndexOf($lookFor, $start+5)
if ($start -gt 0) { 
	$newtxt = $txt.Substring(0, $start) + $lookFor + " " + $versionText + " " + $lookFor + $txt.SubString($end+$lookFor.Length, $txt.Length-$end-$lookFor.Length)
	[Io.File]::WriteAllText($path, $newtxt)
}

#
# Make the .docx file again
#
cd $tempdir 
compress-archive "*" -destinationpath "..\$endzip"
cd ".."
rename-item $endzip $endfile


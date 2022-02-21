using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.Pages
{
    public static class StringConvert
    {
        static readonly string INVALID_CHARS = "\uFFFD";
        static readonly UTF8Encoding Utf8Encoder = new UTF8Encoding(false, false); // false=no BOM false=don't throw on invalid bytes

        static public string ConvertInput(string inputMethod, string inputText, ref List<byte> outputBytes)
        {
            string errstr = "";
            switch (inputMethod)
            {
                case "BASE64-RFC1421":
                case "BASE64-RFC4648":
                case "BASE64-RFC4648url":
                    errstr = ConvertInputBase64(inputMethod, inputText, ref outputBytes);
                    break;

                case "HEX":
                    errstr = ConvertInputHex(inputText, ref outputBytes);
                    break;

                case "ROT-13":
                    errstr = ConvertInputRot13(inputMethod, inputText, ref outputBytes);
                    break;

                case "URL-%20":
                case "URL-+":
                    errstr = ConvertInputUrl(inputMethod, inputText, ref outputBytes);
                    break;

                case "string":
                    outputBytes = Utf8Encoder.GetBytes(inputText).ToList(); ;
                    break;

                default:
                    errstr = $"Error: unknown string conversion type {inputMethod}";
                    break;
            }

            return errstr;
        }

        // https://en.wikipedia.org/wiki/Base64#Variants_summary_table
        class Base64Variation
        {
            public static Base64Variation MakeRFC1421()
            {
                return new Base64Variation() { LineLength = 64 };
            }
            public static Base64Variation MakeRFC4648()
            {
                return new Base64Variation() { }; // all-default is RFC4648 version
            }
            public static Base64Variation MakeRFC4648url()
            {
                return new Base64Variation() { Char62 = '-', Char63 = '_' };
            }
            /// <summary>
            /// Given a string in a non-default Base64 (e.g., uses - instead of +), convert to a stock Base64 encoding.
            /// </summary>
            public string ConvertToDefault(string specializedBase64String)
            {
                var retval = specializedBase64String;
                if (Char62 != DefaultChar62)
                {
                    retval = retval.Replace(Char62, DefaultChar62);
                }
                if (Char63 != DefaultChar63)
                {
                    retval = retval.Replace(Char63, DefaultChar63);
                }
                return retval;
            }

            public string ConvertToSpecialized(string defaultBase64String)
            {
                var retval = defaultBase64String;
                if (Char62 != DefaultChar62)
                {
                    retval = retval.Replace(DefaultChar62, Char62);
                }
                if (Char63 != DefaultChar63)
                {
                    retval = retval.Replace(DefaultChar63, Char63);
                }
                if (LineLength != DefaultLineLength && LineLength > 0)
                {
                    // Just split into line lengths
                    var longstring = retval;
                    retval = "";
                    for (int i = 0; i < longstring.Length; i += LineLength)
                    {
                        var len = Math.Min(longstring.Length - i, LineLength);
                        var sub = longstring.Substring(i, len);
                        if (retval != "") retval += "\r\n";
                        retval += sub;
                    }
                }
                return retval;
            }

            public const char DefaultChar62 = '+';
            public char Char62 = DefaultChar62;
            public const char DefaultChar63 = '/';
            public char Char63 = DefaultChar63;
            public const int DefaultLineLength = 0;
            public int LineLength = DefaultLineLength;
        }
        static readonly Dictionary<string, Base64Variation> Base64Variations = new Dictionary<string, Base64Variation>()
        {
            {  "BASE64-RFC1421", Base64Variation.MakeRFC1421() },
            {  "BASE64-RFC4648", Base64Variation.MakeRFC4648() },
            {  "BASE64-RFC4648url", Base64Variation.MakeRFC4648url() },
        };

        static private string ConvertInputBase64(string inputMethod, string inputText, ref List<byte> outputBytes)
        {
            string errstr = "";
            try
            {
                var variant = Base64Variations[inputMethod];
                var defaultText = variant.ConvertToDefault(inputText);
                outputBytes = Convert.FromBase64String(defaultText).ToList();
            }
            catch (Exception)
            {
                errstr = "Not a valid Base-64 input";
            }
            return errstr;
        }

        static private string ConvertInputHex(string inputText, ref List<byte> outputBytes)
        {
            string errstr = "";

            var list = inputText.Split(new char[] { ' ' });
            bool needsCR = false;
            foreach (var startitem in list)
            {
                var item = startitem;
                if (item.StartsWith("\r") || item.StartsWith("\n"))
                {
                    item = item.Substring(1); // grab rest of string, which might be empty;
                    needsCR = true;
                }
                if (item == "") continue;
                var ok = byte.TryParse(item, NumberStyles.HexNumber, null, out byte b);
                if (ok)
                {
                    if (needsCR)
                    {
                        outputBytes.Add((byte)'\n');
                    }
                    outputBytes.Add(b);
                }
                else
                {
                    errstr = $"ERROR: unknown byte value {item}";
                }
            }
            return errstr;
        }
        static private string ReplaceChar(string text, int value)
        {
            var retval = text;
            if (retval.Contains((char)value))
            {
                retval = retval.Replace($"{(char)value}", $"<{(byte)value:X2}>");
            }
            return retval;
        }

        /// <summary>
        /// Does an in-place conversion of ROT13 input
        /// </summary>
        static private void ConvertBytesRot13(List<byte> outputBytes)
        {
            for (int i = 0; i < outputBytes.Count; i++)
            {
                var b = outputBytes[i];
                if (b >= (byte)'a' && b <= (byte)'z')
                {
                    b = (byte)(b + 13);
                    if (b > (byte)'z') b = (byte)(b - 26);
                    outputBytes[i] = b;
                }
                else if (b >= (byte)'A' && b <= (byte)'Z')
                {
                    b = (byte)(b + 13);
                    if (b > (byte)'Z') b = (byte)(b - 26);
                    outputBytes[i] = b;
                }
            }
        }

        static private string ConvertInputRot13(string inputMethod, string inputText, ref List<byte> outputBytes)
        {
            string errstr = "";
            try
            {
                outputBytes = Utf8Encoder.GetBytes(inputText).ToList();
                ConvertBytesRot13(outputBytes);
            }
            catch (Exception)
            {
                errstr = "Not a valid URL-encoded input";
            }
            return errstr;
        }
        static private string ConvertInputUrl(string inputMethod, string inputText, ref List<byte> outputBytes)
        {
            string errstr = "";
            try
            {
                var outputString = System.Net.WebUtility.UrlDecode(inputText);
                outputBytes = Utf8Encoder.GetBytes(outputString).ToList();
            }
            catch (Exception)
            {
                errstr = "Not a valid URL-encoded input";
            }
            return errstr;
        }

        static public string ConvertOutput(string outputMethod, List<byte> bytes)
        {
            string retval;
            switch (outputMethod)
            {
                case "BASE64-RFC1421":
                case "BASE64-RFC4648":
                case "BASE64-RFC4648url":
                    retval = ConvertOutputBase64(outputMethod, bytes);
                    break;

                case "ROT13":
                    retval = ConvertOutputRot13(outputMethod, bytes);
                    break;

                case "URL-%20":
                case "URL-+":
                    retval = ConvertOutputUrl(outputMethod, bytes);
                    break;

                case "string":
                    retval = ConvertOutputString(bytes);
                    break;

                default:
                    retval = ConvertOutputString(bytes);
                    break;
            }
            return retval;
        }

        static private string ConvertOutputBase64(string subtype, List<byte> bytes)
        {
            string retval;
            var variant = Base64Variations[subtype];
            var defaultText = Convert.ToBase64String(bytes.ToArray());
            retval = variant.ConvertToSpecialized(defaultText);
            return retval;
        }

        static private string ConvertOutputString(List<byte> bytes)
        {
            string retval = "";
            bool printAsAsciiAndHex = false;
            try
            {
                retval = Utf8Encoder.GetString(bytes.ToArray());
                if (retval.Contains(INVALID_CHARS))
                {
                    printAsAsciiAndHex = true;
                }
                for (int i = 0; i < 0x20; i++)
                {
                    retval = ReplaceChar(retval, i);
                }
                retval = ReplaceChar(retval, 0x7F);
            }
            catch (Exception)
            {
                printAsAsciiAndHex = true;
            }
            if (printAsAsciiAndHex)
            {
                // OK -- print the 7bit values as a string, and the 8bit ones in <HEX>
                var sb = new StringBuilder();
                foreach (var b in bytes)
                {
                    if (b >= 0x20 && b < 0x7f)
                    {
                        sb.Append((char)b);
                    }
                    else
                    {
                        sb.Append($"<{b:X2}>");
                    }
                }
                retval = sb.ToString();
            }
            return retval;
        }

        static private string ConvertOutputRot13(string outputMethod, List<byte> bytes)
        {
            string retval = "";
            try
            {
                ConvertBytesRot13(bytes);
                retval = Utf8Encoder.GetString(bytes.ToArray());
            }
            catch (Exception)
            {
                retval = $"ERROR: input is not a valid UTF8 string";
            }
            return retval;
        }

        static private string ConvertOutputUrl(string outputMethod, List<byte> bytes)
        {
            string retval = "";
            try
            {
                var str = Utf8Encoder.GetString(bytes.ToArray());
                retval = System.Net.WebUtility.UrlEncode(str);
                switch (outputMethod)
                {
                    case "URL-+": // this is what .NET does by default
                        break;
                    case "URL-%20":
                        retval = retval.Replace("+", "%20"); // lots of online discussion about which one is right.
                        break;
                }
            }
            catch (Exception)
            {
                retval = $"ERROR: input is not a valid UTF8 string";
            }
            return retval;
        }
        public static int Test()
        {
            int nerror = 0;
            nerror += TestReversable();
            return nerror;
        }
        private static string[] GetTestStrings()
        {
            string[] testStrings = new string[]
            {
                @"Short string",
@"AGAINST THE TIDE

  BY

  JOHN WYCLIFFE
  (Henry Bedford-Jones)


  NEW YORK
  DODD, MEAD AND COMPANY
  1924




  COPYRIGHT, 1924,
  BY DODD, MEAD AND COMPANY, INC.


  PRINTED IN U. S. A.

  VAIL-BALLOU PRESS, INC.
  BINGHAMTON AND NEW YORK




CONTENTS


BOOK I

""The Hidden Things of Dishonesty""


BOOK II

""He Who Did Eat of My Bread""


BOOK III

""A Man's Heart Deviseth His Way""




BOOK I

""THE HIDDEN THINGS OF DISHONESTY""



AGAINST THE TIDE



CHAPTER 1

The old-fashioned Deming mansion, for the hundredth time in its
sedate existence, was filled with a gayety which offset even the
menacing weather.

Although noon was close at hand, the morning was deeply gloomy and
ominous.  Thunder clouds of late summer brooded over the Ohio, and
rain, already sweeping across the wide crescent-bend of the river,
was threatening to burst upon Evansville.  Yet it was not because of
these clouds that the old house was ablaze with light from cellar to
attic.

From the twelve-foot ceilings of the huge rooms hung electric
clusters, whose glare was softened yet quickened by tinkling prisms
and pendants of crystal.  Along the walls twinkled sconces and
candelabra; this richer glow brought out the scarlet coats of
tapestried huntsmen, pursuing stags through indefinite forests of
Gobelin weave.  Everywhere was light and sound and laughter.

A babel of tongues filled the rooms--crisply concise northern speech,
mingled with the softer slur of southern accents.  A listener might
gather that this house was symbolic of Evansville itself, bordering
both north and south, drinking of its best from either section; an
Indiana city, yet of infinite variety, proudly exclusive, living more
in past than present, yet cordial and open-hearthed.

At noon, in this house, Dorothy Deming was to be married to Reese
Armstrong.  The wedding was yet some little distance away.  Macgowan,
who had been dressing for his part of best man and who was a house
guest, crossed the upstairs hall toward the stairway, just as Dorothy
herself appeared from a room which was aflutter with excited feminine
voices.  With the license of his age and position, he led her to the
window-nook and began to speak of Armstrong.  Dorothy, oblivious of
the confusion around, yielded to the detention and listened eagerly.

Why not?  When Lawrence Macgowan spoke, few men but would have
listened; not to mention a bride who was chatting with the groom's
most intimate and trusted friend, and hearing wondrous things about
the man whom she was soon to call her husband.

Macgowan was impressive.  More impressive than J. Fortescue Deming,
in whose features the Deming Food Products Company had seared deep
lines; more impressive than Deming's business directors and social
friends here gathered; more impressive by far than young Armstrong,
whose financial genius was making its mark so rapidly.

Despite the gray at his temples, Lawrence Macgowan possessed a charm
of personality, a steely keenness of intellect, a direct force of
character, which dominated all who came in contact with him.  Being
quite used to making this impression, he made it the more readily.
Men said of Macgowan that he disdained politics, had refused a
supreme court appointment, and preferred corporation law to marriage
as a means of advancement.  True,--perhaps.  Among the doctors of the
law, among those upright ones who lived rigorously by legal ethics
and by ethical illegality, Macgowan moved as a very Gamaliel, honored
in the Sanhedrim and respected by all those whose fortunes his brain
had made.

Men said, too, that some day he would set that brain to making his
own fortune.

""Then,"" Dorothy was inquiring, ""you and Reese are looking this very
minute for some new business to take hold of?  And you haven't found
one?""

Macgowan evaded, smilingly.  His whole person seemed to radiate that
smile as some rich crystal radiates and warms the sunlight, and when
he thus smiled all the strong lines of his face were softened; his
level gaze lost its almost harsh intensity and became winning,
genial, intimate.

""We're not looking, exactly,"" he said.  ""You see, we're more sought
after than seeking--though I should not include myself.  Reese is the
whole thing.  It's his genius that is the very breath of life in
Consolidated.  Do you know that he's put nearly sixteen thousand
investors on our books by his sheer selling ability?  He has actually
sold himself to them.  All small ones, people who can invest only a
few hundred dollars each year.  That is more than an accomplishment;
it is a triumph!""

The girl's cheeks were flushed, her blue eyes shone like stars.

""But it's your help, your faith and backing, which made such big
things possible for him.  To think that he's been in New York only a
year or two!  To think where he will be after ten years--""  Dorothy
broke off, caught her breath sharply, and laughed at her own
enthusiasm.  ""Oh, I'm intoxicated with the very thought of what he's
accomplished and what he will accomplish!  Now tell me about the
companies you-all handle.  Do you buy them and then sell them later
for more money?""

Macgowan shook his head.  ""No.  A manufacturing concern, let us say,
is poorly managed yet essentially sound.  We buy it.  We reorganize
it.  Consolidated Securities owns it and continues to own it.  A
minority of the stock is sold to our investors, to the people who own
Consolidated stock.  It is their privilege to buy stock in this
subsidiary company--""

""The preferred stock?"" cut in Dorothy.  Macgowan chuckled at her
sapient air.

""Yes.  Two shares and no more to each investor, but with these two
shares goes one share of common at a nominal valuation.  Suppose we
start or reorganize two or three such companies in the course of a
year--and we hope to do better than that--the chances are very good
for our investors.  Consolidated sells the stock, owns the subsidiary
company, runs it!  Thus, Consolidated must make sure that the company
will not fail but succeed.  The investor shares the profit with
Consolidated; also, he shares Consolidated's profit from the whole
group of companies.  You see the idea?""

Dorothy nodded quickly, then was checked by Macgowan's air.

""There's just one thing.""  His tone was hesitant, embarrassed.  Her
eyes leaped to his face; his voice seemed to bring a swift
apprehension into her mind.

""Yes?"" she urged him with an eager word.

""There is one thing--""  Macgowan was unaccountably at a loss for
speech; to any who knew him well, an astounding thing.  ""You
understand, the success of Reese Armstrong means everything to me; I
may call myself his closest friend, at least in New York.  And I
know, my dear, that with you at his elbow, with your faith and help
behind him, he is invincible.""

Suspense flashed into the girl's eyes.  This prelude, this slightly
frowning air of embarrassment, hinted at some portentous secret.

""Yes?"" she prompted again.

The lawyer regarded her a long moment, his eyes gravely steady.

""Well, there is one thing I want to say to you; that's why I dragged
you away for a few moments.  Yet I don't want to offend you, my dear.""

""You won't--it's a promise!  What is it?""

""One thing, for his happiness and yours.  He is a wizard at finance;
success has not flung him off balance, for his one thought has ever
been of work.  Now, my dear Dorothy, don't let him drink too deeply
of this wine of wizardry!  No man can serve two masters.  Business
takes its toll of souls, I can assure you; it hardens the spirit,
until nothing is left sacred before its spell.  A man will rob his
best friend in the name of business.  He will take what he can grasp,
and call it finance.  You must see to it that Reese is not too
entirely absorbed in his work--that he is not dominated by the nimble
dollar.""

For a moment the girl met Macgowan's steady gaze, probing for the
meaning underneath his words.  In her eyes rose a question, a quick
protest, an argument.

Then, before she could respond, came a breathless outcry, a swish of
skirts, and two bridesmaids seized upon her.

""Dorothy, you shameless thing!  These brides--they all need a
guardian!  You've driven us perfectly _wild_!  Don't you know that
we've been looking everywhere for you?  It's time you were
dressed--your mother's waiting--""

Dorothy was hustled away in peremptory fashion.

Macgowan, smiling a little to himself, sauntered away and downstairs.
As he entered the great drawing-room he was instantly seized upon.
New guests were each moment arriving and Macgowan, who was to be best
man, was the lion of the hour.  Armstrong had not yet summoned him
for moral support, and he was momentarily free.

This home wedding in its very informality held a formal dignity which
was novel to the New Yorker, and which he found delightful.  Many of
those present were out-of-town house guests, and all were old friends
of the bride; Armstrong had invited only his best man.  Thus the
affair had a strong sense of family intimacy.

Macgowan was quick to feel any psychic and underlying influence.
Behind all this glitter he perceived a curious restraint, a pride, a
singular cool dignity.  Through the babel of voices, underneath the
laughing faces, he was vaguely aware of this thing.  It was as though
many of these people, guests in this house, shared some secret which
they were trying to banish from memory or thought.

Lawrence Macgowan knew exactly what this hidden thing was.

He was no untutored denizen of the metropolis who viewed the country
at large only through the uncertain eyes of the press.  He even had
direct connections with Evansville; across the room he saw his
cousin, Ried Williams, a director and treasurer of the Deming
company.  The relationship was not, however, known to many; even
Armstrong was unaware of it.  Macgowan made his way to the side of
Williams and clapped him on the shoulder.

""Well, Ried?  How are you?""

""Hello, Lawrence!""  The thin, sallow features of Williams suddenly
radiated delight.  ""Here, I want you to meet Pete Slosson, our
assistant general manager.  Pete, this is Lawrence Macgowan; a man to
whom the law is a servitor and shield, the Constitution an act of
providence, and state legislatures mere soda-water bubbles--""

Laughing, Macgowan shook hands with Pete Slosson.  A young man, this,
of singularly clear-cut and intelligent features; yet the eyes were a
bit sullen, the lips a trifle full.  The entire face displayed a
nervous energy not wholly natural.  The man drank.

""Everything Lawrence touches,"" continued Williams warmly, ""and every
one in touch with him, succeeds!  He simply never makes a failure of
anything.""

""Then I'll make a touch,"" Slosson grinned, ""because I'm going to be
broke one of these days.""

Macgowan chuckled.  ""Any time you like,"" he returned.  ""But remember
that the golden touch of Midas went against him at the last!""

One watching these three men closely might have fancied that beneath
their light words lay some deeper significance.

At this moment the negro butler approached.  He deftly bore a huge
tray, upon which crowded tall silver cups, crowned with the rich
green of new mint and steaming frostily from their iced contents.

""Compliments of the bride, gentlemen!"" he addressed them.  ""If
you-all is prohibition, dishyer in de centuh is gwineteed not to
obstruct yo' feelin's or beliefs--""

""Not for us, Uncle Neb!"" Slosson laughed loudly, as he extended one
of the juleps to Macgowan.  ""Here's a treat for you, effete
easterner!  Uncle Neb's cocktails are famous from here to Nashville,
but his juleps are symphonic memories of the good old days.  Take a
long whiff of the mint first, mind; there's only one way to drink a
julep.  That right, Uncle Neb?""

""Dat sho' is de truth, Mistah Slosson!""  The old negro was beaming.
""Yas, suh.  Folks sho' do prove dey quality on de julep.  Ain't dat
de truth, Mistah Slosson?  M-mm!  And Mistah Deming he done growed
dat mint his own self, too--ain't nobody knows mint like he do!""

Macgowan sniffed deeply of the raw fragrance, and raised his goblet.

""Gentlemen, I give you the health of the bride!""

At these words, an almost imperceptible contraction occurred in the
features of Slosson.  Faint as was this movement of the facial
muscles, instantly as it vanished, Macgowan observed it.

After this, he took a deep and lively interest in Pete Slosson; and
Slosson, flattered, talked freely enough.  Any man would have been
flattered to hold the absorbed attention of Macgowan.

""You're wasting your talents here, Slosson,"" said Macgowan at last.
His tone was abrupt and incisive, and confidential in the extreme.
""You ought to have a year or two in Chicago or Indianapolis, handling
bigger things, then come on to New York.  There's no advancement for
a man like you in this town.""

Slosson listened with sulky eyes.

""All very well,"" he returned.  ""But I'm a director, and assistant
general manager of Food Products--which is a big thing here.  If I
went to Indianapolis, where'd I be?  I've no pull up there.""

Macgowan's thin lips curved slightly at this.

""Then you don't care to handle bigger things?""

""Of course I do!"" snapped Slosson.  ""Will you give me a chance at
'em?""

""Yes,"" said Macgowan coolly.  ""Yes.  Not now, though.  Later on--when
some things that are in the air have worked around right.""

""Good!  Then count on me.  Between the two of us, Food Products is
going to pieces soon.""

Macgowan merely nodded indifferently.  ""Why?"" he asked.

Slosson shrugged.

""How the devil should I know?  Business depression, of course.  We
have a good line and it sells, but luck's against us.  There's Deming
now.  Good lord!  Look at his face!""

The two men turned.  Their host had halted in the doorway and was
signing the book of a messenger.  A telegram was in his hand.

Macgowan, not at all astonished by the information just confided to
him, searched the face of Deming.  He read there confirmation of
Slosson's words.  Indubitably, the man was keenly worried.  That
elderly, handsome face was deeply lined with care; a far and
deep-hidden weakness, a frightened panic, was about the eyes.  As he
stood there in the doorway, Deming tore open the envelope and glanced
at the telegram which unfolded in his hand.

Even by the artificial light, Macgowan saw the deathly pallor that
leaped into the man's face; he saw the fingers tremble, saw the
frightful despair that sprang to the eyes.  For one instant Deming
lifted his head, stared blankly around, then turned and was gone.
After him hurried Slosson, concerned and anxious.

Macgowan felt a touch at his elbow.  He turned to find Ried Williams,
who had perceived nothing of this happening in the doorway.  His
rather crafty eyes met the glance of Macgowan with a saturnine air.

""What d'you think of Slosson, Lawrence?""
"
            };
            return testStrings;
        }
        private static int TestReversable()
        {
            int nerror = 0;
            string[] methods = new string[] { 
                "BASE64-RFC4648", "BASE64-RFC1421", "BASE64-RFC4648url",
                "URL-%20", "URL-+",
            };
            string[] testStrings = GetTestStrings();

            foreach (var method in methods)
            {
                foreach (var teststring in testStrings)
                {
                    var summary = teststring.Substring(0, Math.Min(20, teststring.Length));
                    List<byte> bytes = new List<byte>();
                    var errstr = ConvertInput("string", teststring, ref bytes);
                    if (errstr != "")
                    {
                        // Really can't ever trip; string to string bytes will always work.
                        nerror++;
                        System.Diagnostics.Debug.WriteLine($"ERROR: StringConvert method {method} input {summary} failed as input");
                        continue;
                    }

                    var bytesAsString = ConvertOutput(method, bytes);
                    var summaryactual = bytesAsString.Substring(0, Math.Min(20, bytesAsString.Length));

                    List<byte> actualbytes = new List<byte>();
                    errstr = ConvertInput(method, bytesAsString, ref actualbytes);
                    if (errstr != "")
                    {
                        nerror++;
                        System.Diagnostics.Debug.WriteLine($"ERROR: StringConvert method {method} input {summary} failed to convert error {errstr}");
                        continue;
                    }
                    if (bytes.Count != actualbytes.Count)
                    {
                        nerror++;
                        System.Diagnostics.Debug.WriteLine($"ERROR: StringConvert method {method} input {summary} are different lengths {bytes.Count} <> {actualbytes.Count}");
                        continue;
                    }
                    else
                    {
                        for (int i=0; i<bytes.Count; i++)
                        {
                            if (bytes[i] != actualbytes[i] && nerror < 5)
                            {
                                nerror++;
                                System.Diagnostics.Debug.WriteLine($"ERROR: StringConvert method {method} input {summary} item[{i}] s.b. {bytes[i]} actual {actualbytes[i]} ");
                            }
                        }
                    }
                }
            }
            return nerror;
        }
    }
}

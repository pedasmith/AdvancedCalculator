## Scary Alphabet Ouija Board

Controls an Arduino-based pan-and-tilt on which is a TI Bluetooth light.  Result: point at ANYTHING

### Display

Displays messages using a Pan&Tilt controlled light

```BASIC
REM Display a message using the Pan & Tilt
REM The pan and tilt is controlled by an Arduino running
REM the enhanced Ardudroid program with Servo support.

IMPORT FUNCTIONS FROM "TableFunctions"

CLS GREEN
PRINT "Display messages using the Pan and Tilt"


REM Get the pan and tilt device
list = Bluetooth.DevicesRfcommName("HC-06")
PRINT list.Count
IF (list.Count = 0)
    PAPER RED
    PRINT "ERROR: unable to find HC-06 Ardudroid"
    STOP 
END IF
ptraw = list.Get (1)
PRINT ptraw
pt = ptraw.As ("Ardudroid")
pt.ServoAttach (0, 2)
pt.ServoAttach (1, 3)
CONSOLE pt.Name

list = Bluetooth.DevicesName ("beLight*")
lightraw = list.Get(1)
light = lightraw.As ("beLight")
light.SetColor (255, 0, 0, 0)

PRINT "READY!"

tbl = ReadTable()
10 REM Loop Top
message = INPUT DEFAULT "Hello" PROMPT "Enter a message"
message  = String.ToUpper (message)
FOR i  = 1 TO LEN(message)
    ch = MID(message, i, 1)
    pan = GetPan (tbl, ch, -1)
    tilt = GetTilt (tbl, ch, 1)
    IF (pan = -1 OR tilt = -1)
        PRINT "No pan or tilt for " + ch
    ELSE
        light.SetColor (0, 0, 30, 30)
        pt.ServoMove (0, pan)
        pt.ServoMove (1, tilt)
        PAUSE 10
        light.SetColor (255, 0, 0, 0)
        PAUSE 50
        PRINT ch, pan, tilt
    END IF
NEXT i
GOTO 10
```

### Pan Demo

Shows how to use the Slider and Button to point 

```BASIC
CLS BLUE
PRINT "Pan and Tilt"
list = Bluetooth.DevicesRfcommName("HC-06")
PRINT list.Count
IF (list.Count = 0)
    PAPER RED
    PRINT "ERROR: unable to find HC-06 Ardudroid"
    STOP 
END IF
ptraw = list.Get (1)
PRINT ptraw
pt = ptraw.As ("Ardudroid")
pt.ServoAttach (0, 2)
pt.ServoAttach (1, 3)
CONSOLE pt.Name

list = Bluetooth.DevicesName ("beLight*")
lightraw = list.Get(1)
light = lightraw.As ("beLight")

W=400
g = Screen.Graphics (250, 50, 200, W)
g.Background = YELLOW

pan = g.Slider(0, 50, W, 110, "Pan", "pan")
pan.Min = 0
pan.Max = 180
tilt = g.Slider(0, 120, W, 180, "Tilt",  "tilt")
tilt.Min = 0
tilt.Max = 90

BW = 80
stopb = g.Button (0, 0, 70, 30, "STOP", "button")
redb = g.Button (1*BW, 20, 2*BW-5, 60, "RED", "red")
greenb = g.Button (2*BW, 20, 3*BW-5, 60, "GREEN", "green")
blueb = g.Button (3*BW, 20, 4*BW-5, 60, "BLUE", "blue")
offb = g.Button (4*BW, 20, 5*BW-5, 60, "OFF", "off")

FOREVER

FUNCTION button(b)
    FOREVER STOP
END

FUNCTION red(b)
    GLOBAL light
    light.SetColor (255, 0, 0, 0)
END

FUNCTION green(b)
    GLOBAL light
    light.SetColor (0, 255, 0, 0)
END

FUNCTION blue(b)
    GLOBAL light
    light.SetColor (0, 0, 255, 0)
END

FUNCTION off(b)
    GLOBAL light
    light.SetColor (0, 0, 255, 0)
END

FUNCTION pan(s, value)
    Screen.ClearLine (2)
    PRINT "Pan", value
    GLOBAL pt
    pt.ServoMove (0, value)
END
FUNCTION tilt(s, value)
    Screen.ClearLine (2)
    PRINT "Tilt", value
    GLOBAL pt
    pt.ServoMove (1, value)
END
```

### TableFunctions

A set of table functions to create, save, restore and get data from tables.
ReadTable () and SaveTable (tbl)

SetData (tbl, name, pan, tilt)  GetPan (tbl, name, default)  GetTilt (tbl, name, default)


```BASIC
REM Functions to make, use, save, restore tables
REM MakeEmptyTable
REM ...SetData (tbl, name, pan, tilt)
REM ...GetPan (tbl, name, default)
REM ...GetTilt (tbl, name, default)
REM
REM ReadTable ()
REM SaveTable (tbl)
REM
REM ...FindRow --> Index or -1
REM ...
CLS BLUE
PRINT "Test TableFunctions"
TEST()


FUNCTION MakeEmptyTable ()
    DIM tbl()
    tbl.AddRow ("Name", "Pan", "Tilt")
    RETURN tbl
END

FUNCTION FindRow (tbl, name)
    maxrow = tbl.Count
    retval = -1
    FOR r = 2 TO maxrow
        row = tbl[r]
        rowname = row[1]
        IF (rowname = name) THEN RETURN r
    NEXT r
    RETURN retval
END

FUNCTION GetPan (tbl, name, default)
    r = FindRow (tbl, name)
    retval = default
    IF (r <> -1)
        row = tbl[r]
        retval = row[2]
    END IF
    RETURN  retval
END

FUNCTION GetTilt (tbl, name, default)
    r = FindRow (tbl, name)
    IF (r = -1)
        RETURN default
    ELSE
        row = tbl[r]
        RETURN row[3]
    END IF
RETURN


FUNCTION SetData (tbl, name, pan, tilt)
    r = FindRow (tbl, name)
    IF (r = -1)
        tbl.AddRow (name, pan, tilt)
    ELSE
        row = tbl[r]
        row[2] = pan
        row[3] = tilt
    END IF
RETURN

FUNCTION ReadTable ()
    str = String.Escape ("csv", tbl)
    file = File.ReadPicker(“.csv”)
    IF (file.IsError)
        REM file will have a error message
        PRINT "Cannot open filee", file
        RETURN
    END IF
    str = file.ReadAll()
    tbl = String.Parse ("csv", str)
RETURN tbl

FUNCTION SaveTable (tbl)
    str = String.Escape ("csv", tbl)
    file = File.WritePicker("CSV file", “.csv”, "test.csv")
    IF (file.IsError)
        REM file will have a error message
        PRINT "Cannot open filee", file
        RETURN
    END IF
    file.WriteText (str)
RETURN


FUNCTION Test_Add_Row_Twice()
    nerror = 0
    tbl = MakeEmptyTable()

    pan = GetPan (tbl, "A", -1)
    nerror = nerror + ASSERT(pan, -1, "no rows result is default")

    SetData (tbl, "A", 10, 200)
    pan = GetPan (tbl, "A", -1)
    nerror = nerror + ASSERT(pan, 10, "first row pan is 10")

    SetData (tbl, "A", 11, 201)
    pan = GetPan (tbl, "A", -1)
    nerror = nerror + ASSERT(pan, 11, "pan should be 11")

    SetData (tbl, "B", 20, 120)
    SetData (tbl, "C", 30, 130)
    SaveTable (tbl)
    tbl2 = ReadTable ()
    pan = GetPan (tbl2, "B", -1)
    nerror = nerror + ASSERT(pan, 20, "tbl2 pan B")
    pan = GetPan (tbl2, "C", -1)
    nerror = nerror + ASSERT(pan, 30, "tbl2 pan C")

    RETURN nerror
END

FUNCTION TEST()
    nerror = 0
    nerror = nerror + Test_Add_Row_Twice()
    RETURN nerror
END

FUNCTION ASSERT (actual, expected, str)
    IF (actual = expected) 
        CONSOLE "OK: " + str
        RETURN 0
    END IF
    CONSOLE "ERROR: "+str
    CONSOLE "Variable is " + actual + " but should be " + expected
    RETURN 1
END
```

### Training

Trains the Pan and Tilt device, giving names (like "A") to different positions

```BASIC
REM Train the Pan & Tilt
REM The pan and tilt is controlled by an Arduino running
REM the enhanced Ardudroid program with Servo support.

IMPORT FUNCTIONS FROM "TableFunctions"

CLS GREEN
PRINT "Train the Pan and Tilt"
tbl = MakeEmptyTable()
Pan= 0
Tilt = 0

H = 300
W = 400
g = Screen.Graphics(50, 50, 300, 400)
g.Background = YELLOW
pan = g.Slider (0, 220, W, 280, "pan", "OnPan")
pan.Max  = 180
tilt = g.Slider(0, 150, W, 210, "tilt", "OnTilt")
tilt.Max = 90
readb = g.Button(0, 0, 70, 40, "Read", "OnRead")
trainb = g.Button(80, 0, 150, 40, "Train", "OnTrain")
saveb = g.Button(160, 0, 230, 40, "Save", "OnSave")
debugb = g.Button(240, 0, 310, 40, "debug", "OnDebug")


REM Get the pan and tilt device
list = Bluetooth.DevicesRfcommName("HC-06")
PRINT list.Count
IF (list.Count = 0)
    PAPER RED
    PRINT "ERROR: unable to find HC-06 Ardudroid"
    STOP 
END IF
ptraw = list.Get (1)
PRINT ptraw
pt = ptraw.As ("Ardudroid")
pt.ServoAttach (0, 2)
pt.ServoAttach (1, 3)
CONSOLE pt.Name

list = Bluetooth.DevicesName ("beLight*")
lightraw = list.Get(1)
light = lightraw.As ("beLight")
light.SetColor (255, 0, 0, 0)

PRINT "READY!"

FOREVER

FUNCTION OnRead(b)
    GLOBAL tbl
    tbl = ReadTable()
    CONSOLE "Read table " + tbl
RETURN

FUNCTION OnDebug(b)
    GLOBAL tbl
    CONSOLE "Current table " + tbl
RETURN

FUNCTION OnSave(b)
    GLOBAL tbl
    SaveTable (tbl)
RETURN

FUNCTION OnTrain(b)
    GLOBAL tbl
    GLOBAL Pan
    GLOBAL Tilt
    label = INPUT DEFAULT "A" PROMPT "What letter?"
    SetData (tbl, label, Pan, Tilt)
    CONSOLE "Current table " + tbl
RETURN


FUNCTION OnPan(s, value)
    GLOBAL Pan
    Pan = value
    Screen.ClearLine (2)
    PRINT "Pan", Pan
    GLOBAL pt
    pt.ServoMove (0, Pan)
END

FUNCTION OnTilt(s, value)
    GLOBAL Tilt
    Tilt = value
    Screen.ClearLine (2)
    PRINT "Tilt", Tilt
    GLOBAL pt
    pt.ServoMove (1, Tilt)
END
```



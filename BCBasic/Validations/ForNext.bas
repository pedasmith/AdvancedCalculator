value = testloop()
PRINT value



FUNCTION testloop ()
retval = 0
FOR i = 1 TO 10
FOR j = 1 TO i
	retval = i*10 + j
NEXT j
NEXT i
RETURN retval

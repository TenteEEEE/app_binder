# AppBinder
AppBinder is an application-binding program. 
This is not a general application launcher.  
It acheves fault detection/recover and application persistence.

# What is Application-Binding?
## I need a notepad when running calculators
Bind Calculator.exe and notepad.exe

## I need Excel too
We just bind a notepad.exe and Excel.exe

## An application must be persistent
It supports restart policy such as "on failure" and "always".

# Something went wrong?
## Trigger does not work
If a process name of "something.exe" is different from "something", it does not work now. 
Please set the correct process name on the "Trigger EXE/Process" manually.

## I killed a trigger process but binding program is still running
Probably, the trigger program's exit code is non-zero value(something fault).  
If it is your application, please modify the return number.  
When I get a motivation to make "exception exit-code", it will be better.

## Bidirectional Binding
I am considering this.  
It works when we add a opposite binding as a new config now, but it is not beautiful.
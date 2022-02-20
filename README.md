[*>------------------------------------------------------------------------<*]

Why this thing exists.
----------------------

There's an astonishing amount of software out there that was written in 
support of quite a number of the BBS programs in the BBS Software Collection, 
but there is a lot of manual labor involved in getting all that material into 
a format that can be easily navigated.  This tool is designed to process a 
directory filled with support tools for a specific system.  It can parse 
multiple concatenated file listings in different formats and will output HTML 
that can be used to browse the file collection.

process_file_desc takes a single command line parameter - the name of the 
file that contains the filenames and descriptions for all the items in the
directory.  If you run the tool from a directory different than the one where 
the files & their description file live, you'll need to specify that.
For example, if the content & description file lives in \wildcat\support, you
will need to include that pathname on the commandline, for example:

    process_file_desc \wildcat\support\files.bbs

Most of the time collections of support software will be found on various
shareware CD collections.  Many of these collections are designed to be 
"BBS friendly", meaning that their layout and contents are designed to be
easily imported into the file or download area of a BBS.  Most of these include
a file called "FILES.BBS" or "DIRn" (where n is a directory number).  This file
typically contains the name of the file, the file size, date, and one or more 
lines of text describing each entry in the file.  This file is going to be
used as the source input to process_file_desc.  

Before a description file can be processed, you'll need to edit the file 
and remove any unrelated information that may be at the top or the bottom of 
the file.  This material is most often an area description or other text that's 
not directly describing a file in the list.

Next, you'll need to determine the starting column number for each of the 
four fields that process_file_desc can look for - file name, file size, file 
date, and file description.  It's important to note that not all file 
description files need all four columns of information.  Some may only include 
the file name and the file description.  parse_file_desc will derive the file 
size and file date if that information isn't available in the description file.

An example from a description file looks like this:

    175B917A.ZIP  [0]              RBBS-PC 17.5á-Wide Area BETA Release.
                                   | Wide-Area-BETA UPDATE to RBBS-PC 17.4 This
                                   | file contains RBBS-PC.EXE optimized for
                                   | AT-Class machines (80286 and better)
    175B917C.ZIP  [0]              RBBS-PC 17.5á-Wide Area BETA Release.
                                   | Wide-Area-BETA UPDATE to RBBS-PC 17.4 This
                                   | file contains the new CONFIG.EXE Includes an
                                   | editable on-line help file. Officially
                                   | Authorized by Ken Goosens. 

This example was taken from the "BBS Pgm & Util" directory from the shareware
CD "Nightowl #14".  As you can see, it's got information we don't want and 
also contains multi-line description blocks.

To allow process_file_desc to process this particular file, you'll need to add 
information to the top of the file that describes the column positions, the
date separator that you either want to use, or that the original file used, 
as well as the long & short names of the support collection being processed.

If a description file is missing a column, list the position as "0".  For our
example above, the column description line to be added is this:
1,0,0,32,-

The filename begins in position 1, there's no file or date columns, and the
description column begins at position 32.  Finally, I've specified "-" as the
date separation character.  The program will generate dates in the mm-dd-yy
format.

The next two lines define the long & short names of this area:
RBBS-PC
RBBS-PC BBS Support Software & Information

After that, is the actual file descriptions. Here's what it looks like 
all combined:

    1,0,0,32,-
    RBBS-PC
    RBBS-PC BBS Support Software & Information
    175B917A.ZIP  [0]              RBBS-PC 17.5á-Wide Area BETA Release.
                                   | Wide-Area-BETA UPDATE to RBBS-PC 17.4 This
                                   | file contains RBBS-PC.EXE optimized for
                                   | AT-Class machines (80286 and better)
    175B917C.ZIP  [0]              RBBS-PC 17.5á-Wide Area BETA Release.
                                   | Wide-Area-BETA UPDATE to RBBS-PC 17.4 This
                                   | file contains the new CONFIG.EXE Includes an
                                   | editable on-line help file. Officially
                                   | Authorized by Ken Goosens. 

It's also possible to concatenate any number of different description files
together for the same support collection.  To do this, add a "tear line", which
consists of " --" (space, and two dashes) separating one file description block
from the next.  After the tear line, you'd add the new column description.
The previously specified short & long section names will be used for this
new section, so make sure you're not combining disparate things - like sysop
utilities and door games for example.

Below is an example of combining different description files:

    1,0,0,32,-
    RBBS-PC
    RBBS-PC BBS Support Software & Information
    175B917A.ZIP  [0]              RBBS-PC 17.5á-Wide Area BETA Release.
                                   | Wide-Area-BETA UPDATE to RBBS-PC 17.4 This
                                   | file contains RBBS-PC.EXE optimized for
                                   | AT-Class machines (80286 and better)
    175B917C.ZIP  [0]              RBBS-PC 17.5á-Wide Area BETA Release.
                                   | Wide-Area-BETA UPDATE to RBBS-PC 17.4 This
                                   | file contains the new CONFIG.EXE Includes an
                                   | editable on-line help file. Officially
                                   | Authorized by Ken Goosens. 
    --
    1,13,24,32,-
    10VIDEOS.ZIP     7027  12-04-93  Top Ten Movie Video Rental Bulletin
                                   | generator.
    1LPPL110.ZIP     7899  06-19-93  Oneliners v1.10; PPL Version Displays
                                   | oneliners to user, asks if he would like to
                                   | add one, shows node display. Displays Last
                                   | Few Callers. For use with PCBoard 15.0
    3DTIME11.ZIP    26983  10-09-93  3DTIME11; 3D Time Log is designed to do one
                                   | simple task, keep track of the elapsed time
                                   | spent in a door or hook. 3D Time Log appends
                                   | the elapsed time info to a file named in the
                                   | same directory it is executed from.
    ACTSCAN1.ZIP    40592  12-31-93  ActivityScan v1.0 - Caller List Maker


While unrelated, the example above shows how you can combine two wildly
different description files into one, for easy processing.


[*>------------------------------------------------------------------------<*]

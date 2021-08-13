using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Microsoft.VisualBasic.CompilerServices;

namespace process_file_desc {
    class Program {
        // This is a tool to process FILES.BBS or other file listings
        // typically seen on shareware CD-ROMs.
        // Output is to stdout, expecting to be redirected to an html file
        // (typically index.html)

        static void pError(string errMsg) {
            // write an error msg to stderr.
            TextWriter stdErr = Console.Error;
            stdErr.WriteLine(errMsg);
        }

        static DateTime TextToDate(string inText, char sepChar) {
            // takes a date like mm-dd-yy or mm/dd/yy (or whatever sepChar is)
            // and returns a date type.
            string[] workDate = inText.Split(sepChar);
            int century;
            if (System.Convert.ToInt32(workDate[2]) >= 75) {
                century = 1900;
            } else {
                century = 2000;
            }
            return (new DateTime(century + System.Convert.ToInt32(workDate[2]),
                System.Convert.ToInt32(workDate[0]),
                System.Convert.ToInt32(workDate[1])));
        }

        static void Main(string[] args) {
            List<string> skipList = new List<string>();
            if (args.Length != 4) {
                Console.WriteLine("Usage:");
                Console.WriteLine("process_file_desc <desc file> <short description> <long description> <field list> > output.html");
                Console.WriteLine("");
                Console.WriteLine("Example:");
                Console.WriteLine("process_file_desc FILES.BBS \"PC Board\" \"PC Board BBS Software\" 1,13,24,34,- > index.html");
                Console.WriteLine("");
                Console.WriteLine(
                    "In the above example, the field list dictates that the filename will start in column 1,");
                Console.WriteLine(
                    "the file size in column 13, file date in column 24, and the description in column 34.");
                Console.WriteLine("Finally, the date separation character '-' is listed last.");
                Console.WriteLine("");
                Console.WriteLine("If a column isn't present in the description file, use '0' to indicate the starting");
                Console.WriteLine("position. For example, if the file is missing the file date, you would pass ");
                Console.WriteLine("1,13,0,34,/ on the command line (assuming the description starts at 34)");

                Console.WriteLine("");
                Console.WriteLine("Note that if the directory file you specify is not in your current working");
                Console.WriteLine("directory, the files it describes must be in the same location as the description");
                Console.WriteLine("file.  For example, if your description file is c:\\files\\stuff\\files.bbs, then the");
                Console.WriteLine("files described by files.bbs must be in c:\\files\\stuff.");
                Console.WriteLine("");
                Console.WriteLine("");
            } else {
                string descFilename = args[0];
                string shortDescription = args[1];
                string longDescription = args[2];
                string[] fieldLocs = args[3].Split(",");
                if (fieldLocs.Length != 5) {
                    pError($"Invalid number of field parameters, {args[3]}.");
                    pError($"Needed 5, found {fieldLocs.Length}.");
                    return;
                }
                // don't want to count what we're reading or what we're creating.
                skipList.Add("INDEX.HTML"); // presumably this is what is being output to.
                skipList.Add(Path.GetFileName(descFilename).ToUpper());


                ProcessFileArea(descFilename, shortDescription, longDescription, fieldLocs);

            }

            void ProcessFileArea(string descFilename, string shortDescription, string longDescription,
                string[] fieldLocs) {
                StreamReader descReader;
                classFileEntry workFile;


                FileInfo testFile;

                Dictionary<String, classFileEntry> processedFileList = new Dictionary<string, classFileEntry>();
                string workingPath = Path.GetDirectoryName(descFilename);
                if (workingPath.Equals("")) {
                    workingPath = Directory.GetCurrentDirectory();
                }
                DirectoryInfo workingDir = new DirectoryInfo(workingPath);

                
                descReader = new StreamReader(descFilename);
                
                EmitHeader(shortDescription, longDescription);

                // If the file description field is 0 or blank when read from the description file,
                // we should automatically try to find a file_id.diz or descript.ion file and use that.
                // 

                int filePos;
                int sizePos;
                int datePos;
                int descPos;
                char sepChar = fieldLocs[4].ToCharArray()[0];

                if (int.TryParse(fieldLocs[0], out filePos)) {
                    filePos--;  // because we count from zero...
                } else {
                    pError($"Filename position given ({fieldLocs[0]}) is invalid.");
                    return;
                }
                if (int.TryParse(fieldLocs[1], out sizePos)) {
                    sizePos--;  // because we count from zero...
                } else {
                    pError($"File size position given ({fieldLocs[1]}) is invalid.");
                    return;
                }
                if (int.TryParse(fieldLocs[2], out datePos)) {
                    datePos--;  // because we count from zero...
                } else {
                    pError($"File date position given ({fieldLocs[2]}) is invalid.");
                    return;
                }
                if (int.TryParse(fieldLocs[3], out descPos)) {
                    descPos--;  // because we count from zero...
                } else {
                    pError($"File description position given ({fieldLocs[3]}) is invalid.");
                    return;
                }



                string checkName;
                string holdFile = "";
                string workLine;
                string fileName;
                string fileSize;
                string fileDate;
                string fileDesc;
                long totalSize = 0L;

                while (!descReader.EndOfStream) {
                    workLine = descReader.ReadLine();
                    //                            Some files.bbs and other file listings may have
                    //                            "non descriptive" text that leads with a space,
                    //                            and is typically something we need to ignore.
                    if (workLine.Trim() != "" && !workLine.Substring(0, 1).Equals(" ")) {
                        fileName = workLine.Trim().Substring(filePos, 12).ToUpper();
                        if (workingPath.Trim() == "") {
                            checkName = fileName;
                        } else {
                            checkName = workingPath + "\\" + fileName;
                        }

                        if (File.Exists(checkName) || fileName.Equals("")) {
                            if (fileName.Equals("")) {
                                if (!holdFile.Equals("")) {
                                    // chances are pretty high that this is part of an extended or multi-line
                                    // description for a file we just added.
                                    processedFileList[holdFile].FileDesc += workLine.Trim().Substring(descPos);
                                }
                            } else {
                                testFile = new FileInfo(checkName);
                                fileSize = testFile.Length.ToString("N0");
                                totalSize += testFile.Length;
                                if (datePos == -1) {
                                    fileDate = File.GetLastWriteTime(checkName).ToShortDateString();
                                } else {
                                    fileDate = TextToDate(workLine.Substring(datePos, 8), sepChar).ToShortDateString();
                                }

                                if (workLine.Length >= descPos) {
                                    fileDesc = workLine.Trim().Substring(descPos);
                                } else {
                                    fileDesc = "NO DESCRIPTION FOUND";
                                }

                                workFile = new classFileEntry(fileName, fileSize, fileDate, fileDesc);

                                if (!processedFileList.ContainsKey(fileName)) {
                                    processedFileList.Add(fileName, workFile);
                                } else {
                                    pError($"{fileName} was found more than once in the working directory.");
                                    pError($"Please edit the {descFilename} file to remove the duplicate.");
                                }
                            }

                            holdFile = fileName;
                        }
                    }
                }

                if (processedFileList.Count > 0) {
                    foreach (string fName in processedFileList.Keys) {
                        workFile = processedFileList[fName];
                        if (workFile.FileDesc.Contains("<br>")) {
                            string tempStr = $"<br>{workFile.FileDesc}<br>";
                            workFile.FileDesc = tempStr;
                        }

                        EmitDetail(workFile);

                    }

                }
                EmitFooter(processedFileList.Count, totalSize);
            }

            static void EmitHeader(string shortDescription, string longDescription) {
                // this is the "standard" support files listing format.
                Console.WriteLine("<HTML>");
                Console.WriteLine($"<TITLE> BBS Documentary Software: {shortDescription} </TITLE>");
                Console.WriteLine(
                    "<BODY BGCOLOR=\"#FFFFFF\" TEXT=\"#000000\" LINK=\"#444444\" ALINK=\"#999999\" VLINK=\"#999999\">");
                Console.WriteLine("<TABLE WIDTH=100%>");
                Console.WriteLine($"<TD WIDTH=100% BGCOLOR=#000000><FONT COLOR=#FFFFFF SIZE=+2><B>{longDescription} Support Files</B><BR></FONT></TD>");
                Console.WriteLine("</TABLE>");
                Console.WriteLine("<TABLE WIDTH=100%>");
                Console.WriteLine("<TD BGCOLOR=#000000><FONT COLOR=#FFFFFF><B>Filename</B><BR></FONT></TD>");
                Console.WriteLine("<TD BGCOLOR=#000000><FONT COLOR=#FFFFFF><B>Size</B><BR></FONT></TD>");
                Console.WriteLine("<TD BGCOLOR=#000000><FONT COLOR=#FFFFFF><B>Date</B><BR></FONT></TD>");
                Console.WriteLine("<TD BGCOLOR=#000000><FONT COLOR=#FFFFFF><B>Description of the File</B><BR></TD></TR>");
                //Console.WriteLine("<tab indent=60 id=T><br>");

            }

            static void EmitDetail(classFileEntry workFile) {
                Console.WriteLine(
                    $"<TR VALIGN=TOP><TD ALIGN=TOP><A HREF=\"{workFile.FileName}\">{workFile.FileName}</A><TD>{workFile.FileSize}</td><TD>{workFile.FileDate}</td><td>{workFile.FileDesc}</td></tr>");
            }
            static void EmitFooter(int fileCount, long totalSize) {
                Console.WriteLine($"</TABLE><P><TABLE WIDTH=100%><TR><TD ALIGN=RIGHT><SMALL> There are {fileCount} files for a total of {totalSize.ToString("N0")} bytes.</SMALL></TABLE>");
            }
        }
    }
}

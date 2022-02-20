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
                Console.WriteLine("process_file_desc <desc file> > output.html");
                Console.WriteLine("");
                Console.WriteLine("Example:");
                Console.WriteLine("process_file_desc FILES.BBS > index.html");
                Console.WriteLine("");
                
                Console.WriteLine("");
                Console.WriteLine("Note that if the directory file you specify is not in your current working");
                Console.WriteLine("directory, the files it describes must be in the same location as the description");
                Console.WriteLine("file.  For example, if your description file is c:\\files\\stuff\\files.bbs, then the");
                Console.WriteLine("files described by files.bbs must be in c:\\files\\stuff.");
                Console.WriteLine("");
                Console.WriteLine("");
            } else {
                string descFilename = args[0];
                //string shortDescription = args[1];
                //string longDescription = args[2];
                //string[] fieldLocs = args[3].Split(",");
                //if (fieldLocs.Length != 5) {
                //    pError($"Invalid number of field parameters, {args[3]}.");
                //    pError($"Needed 5, found {fieldLocs.Length}.");
                //    return;
                //}
                // don't want to count what we're reading or what we're creating.
                skipList.Add("INDEX.HTML"); // presumably this is what is being output to.
                skipList.Add(Path.GetFileName(descFilename).ToUpper());


                ProcessFileArea(descFilename); //, shortDescription, longDescription, fieldLocs);

            }

            void ProcessFileArea(string descFilename) {
                StreamReader descReader;
                classFileEntry workFile;
                string shortDescription;
                string longDescription;
                string[] fieldLocs;
                FileInfo testFile;

                Dictionary<String, classFileEntry> processedFileList = new Dictionary<string, classFileEntry>();
                string workingPath = Path.GetDirectoryName(descFilename);
                if (workingPath.Equals("")) {
                    workingPath = Directory.GetCurrentDirectory();
                }
                DirectoryInfo workingDir = new DirectoryInfo(workingPath);


                descReader = new StreamReader(descFilename);

                

                // If the file description field is 0 or blank when read from the description file,
                // we should automatically try to find a file_id.diz or descript.ion file and use that.
                // 

                int filePos = 0;
                int sizePos = 0;
                int datePos = 0;
                int descPos = 0;
                char sepChar = ' ';

                string checkName;
                string holdFile = "";
                string workLine;
                string fileName;
                string fileSize;
                string fileDate;
                string fileDesc;
                long totalSize = 0L;
                int lineCount = 0;

                while (!descReader.EndOfStream) {
                    workLine = descReader.ReadLine();
                    lineCount++;
                    if ((lineCount == 1 || workLine.Equals(" --")) && workLine.Contains(",")) {
                        fieldLocs = workLine.Split(',');

                        if (!ParseColumnFields(fieldLocs, out filePos, out sizePos, out datePos, out descPos, out sepChar))
                            return;
                                                
                        if (lineCount == 1) {
                            shortDescription = descReader.ReadLine();
                            longDescription = descReader.ReadLine();
                            lineCount += 1;
                            EmitHeader(shortDescription, longDescription);
                        } else {
                            // we got a tear line, so we need to pull new column defs.
                            string tempStr = descReader.ReadLine();
                            fieldLocs = tempStr.Split(',');
                            if (!ParseColumnFields(fieldLocs, out filePos, out sizePos, out datePos, out descPos,
                                out sepChar))
                                return;
                        }
                    }
                    //
                    // Some files.bbs and other file listings may have
                    // "non descriptive" text that leads with a space,
                    // and is typically something we need to ignore.
                    //
                    if (workLine.Trim().Equals("") || workLine.Substring(0, 1).Equals(" "))
                        continue;
                    
                    fileName = workLine.Trim().Substring(filePos, 12).ToUpper();
                    if (workingPath.Trim() == "") {
                        checkName = fileName;
                    } else {
                        checkName = workingPath + "\\" + fileName;
                    }

                    if (!File.Exists(checkName))
                        continue;


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
                Console.WriteLine("<html>");
                Console.WriteLine($"<title> BBS Documentary Software: {shortDescription} </title>");
                Console.WriteLine(
                    "<body bgcolor=\"#FFFFFF\" text=\"#000000\" link=\"#444444\" alink=\"#999999\" vlink=\"#999999\">");
                Console.WriteLine("<table width=100%>");
                Console.WriteLine($"  <td width=100% bgcolor=#000000><font color=#FFFFFF SIZE=+2><b>{longDescription} Support Files</b></font></td>");
                Console.WriteLine("</table>");
                Console.WriteLine("<table width=100%>");
                Console.WriteLine("  <tr>");
                Console.WriteLine("    <td bgcolor=#000000><font color=#FFFFFF><b>Filename</b></font></td>");
                Console.WriteLine("    <td bgcolor=#000000><font color=#FFFFFF><b>Size</b></font></td>");
                Console.WriteLine("    <td bgcolor=#000000><font color=#FFFFFF><b>Date</b></font></td>");
                Console.WriteLine("    <td bgcolor=#000000><font color=#FFFFFF><b>Description of the File</b></td>");
                Console.WriteLine("  </tr>");
            }

            static void EmitDetail(classFileEntry workFile) {
                Console.WriteLine(
                    $"  <tr valign=top><td align=top><a href=\"{workFile.FileName}\">{workFile.FileName}</A><td align=right>{workFile.FileSize}</td><td>{workFile.FileDate}</td><td>{workFile.FileDesc}</td></tr>");
            }
            static void EmitFooter(int fileCount, long totalSize) {
                Console.WriteLine("</table><p>");
                Console.WriteLine($"<table width=100%><tr><td align=right><small>There are {fileCount} files for a total of {totalSize.ToString("N0")} bytes.</small></td></tr></table>");
                Console.WriteLine("</body>");
                Console.WriteLine("</html>");
            }
        }

        private static bool ParseColumnFields(string[] fieldLocs,
            out int filePos, out int sizePos, out int datePos, out int descPos,
            out char sepChar) {
            bool hasError = false;
            if (int.TryParse(fieldLocs[0], out filePos)) {
                filePos--;  // because we count from zero and meatbags don't.
            } else {
                pError($"Filename position given ({fieldLocs[0]}) is invalid.");
                hasError = true;
            }
            if (int.TryParse(fieldLocs[1], out sizePos)) {
                sizePos--;  
            } else {
                pError($"File size position given ({fieldLocs[1]}) is invalid.");
                hasError = true;
            }
            if (int.TryParse(fieldLocs[2], out datePos)) {
                datePos--;
            } else {
                pError($"File date position given ({fieldLocs[2]}) is invalid.");
                hasError = true;
            }
            if (int.TryParse(fieldLocs[3], out descPos)) {
                descPos--;
            } else {
                pError($"File description position given ({fieldLocs[3]}) is invalid.");
                hasError = true;
            }
            if (hasError) {
                filePos = -1;
                sizePos = -1;
                datePos = -1;
                descPos = -1;
                sepChar = ' ';
                return false;
            }
            sepChar = fieldLocs[4].ToCharArray()[0];
            return true;

        }
    }
}

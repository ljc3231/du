// See https://aka.ms/new-console-template for more information

using System;
using System.IO; //For checking if directory provided is valid
using System.Diagnostics; //Stopwatch
using System.Collections.Generic;
using System.Collections.ObjectModel;


class Program
{

    //Globals


    static void Main(string[] args)
    {
        string dir = "";
        string param = "";
        // Check if arguments are passed
        if (args.Length == 2)
        {
            //Console.WriteLine("Arguments passed:");
            param = args[0];
            dir = args[1];
            //Console.WriteLine($"Dir = {dir}, type = {param}");
        }
        else
        {
            Console.WriteLine("Incorrect amount of parameters provided");
            Console.WriteLine(@"Usage: du [-s] [-d] [-b] <path>
            Summarize disk usage of the set of FILES, recursively for directories.
            You MUST specify one of the parameters, -s, -d, or -b
            -s Run in single threaded mode
            -d Run in parallel mode (uses all available processors)
            -b Run in both parallel and single threaded mode.
            Runs parallel followed by sequential mode                                  
            ");
        }

        if(Directory.Exists(args[1])){
            dir = args[1];
        }
        else{
            Console.WriteLine("Provided directory is not on your system.");
            Console.WriteLine(@"Usage: du [-s] [-d] [-b] <path>
            Summarize disk usage of the set of FILES, recursively for directories.
            You MUST specify one of the parameters, -s, -d, or -b
            -s Run in single threaded mode
            -d Run in parallel mode (uses all available processors)
            -b Run in both parallel and single threaded mode.
            Runs parallel followed by sequential mode                                  
            ");
        }

        if(param == "-s"){
            //run sequential
            sequential(dir);
        }
        else if (param == "-d"){
            //run parallel
            parallel(dir);
        }
        else if (param == "-b"){
            //run parallel, then sequential
            parallel(dir);
            sequential(dir);
        }
        else{
            Console.WriteLine("Incorrect parameter provided.");
            Console.WriteLine(@"Usage: du [-s] [-d] [-b] <path>
            Summarize disk usage of the set of FILES, recursively for directories.
            You MUST specify one of the parameters, -s, -d, or -b
            -s Run in single threaded mode
            -d Run in parallel mode (uses all available processors)
            -b Run in both parallel and single threaded mode.
            Runs parallel followed by sequential mode                                  
            ");
        }

    }

    //Provided a directory, recursively goes through folders to count files
    static void sequentialCountFilesFolders(string dir, ref int fileCount, ref int folderCount, ref long totalSize, ref int imgFilesCount, ref long imgSize){
        try{
            //Count files
            string[] files = Directory.GetFiles(dir);
            fileCount += files.Length;

            foreach(string file in files){
                if(File.Exists(file)){
                    FileInfo f = new FileInfo(file);
                    totalSize +=  f.Length;
                    //Check for ext .jpeg, .png, .jpg (for images)
                    if(Path.GetExtension(file) == ".png" || Path.GetExtension(file) == ".jpeg" || Path.GetExtension(file) == ".jpg" ){
                        imgSize += f.Length;
                        imgFilesCount += 1;
                    }
                }
                else{
                    Console.WriteLine($"File '{file} not found.");
                }
            }

            //Count subdirectories
            
            string[] directories = Directory.GetDirectories(dir);
            folderCount += directories.Length;

            foreach(string directory in directories){
                sequentialCountFilesFolders(directory, ref fileCount, ref folderCount, ref totalSize, ref imgFilesCount, ref imgSize);
            }
        }catch (Exception e){
            //Console.WriteLine($"Inaccessible Directory: '{e}");
        }
    }

    static void sequential(string dir){
        double time;
        int folders = 0;
        int allFiles = 0;
        long bytes = 0;
        int imgFiles = 0;
        long imgBytes = 0;


        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        if(Directory.Exists(dir)){
            //start of recursive calls
            sequentialCountFilesFolders(dir, ref allFiles, ref folders, ref bytes, ref imgFiles, ref imgBytes);
        } else{
            Console.WriteLine($"Directory '{dir}' does not exist.");
        }
        //End of run
        stopwatch.Stop();
        time = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine();
        Console.WriteLine($"Sequential Calculated in: '{time}' seconds");
        Console.WriteLine($"'{folders}' folders, '{allFiles}' files, '{bytes}' bytes");
        Console.WriteLine($"'{imgFiles}' image files, '{imgBytes}' bytes");

    }

    static int foldersP = 0;
    static int allFilesP = 0;
    static long bytesP = 0;
    static int imgFilesP = 0;
    static long imgBytesP = 0;

    static void parallelCountFilesFolders(string dir){
        //Console.WriteLine($"Looking at dir: '{dir}'");
        

        try {
            string[] files = Directory.GetFiles(dir);
            string[] directories = Directory.GetDirectories(dir);

            //allows me to do these atomically, allowing them to be safe in parallel
            //code is not from AI, however it helped me to learn more about how to use Interlocked safely
            Interlocked.Add(ref allFilesP, files.Length);
            Interlocked.Add(ref foldersP, directories.Length);


            //file processing for size, and images
            Parallel.ForEach(files, file =>
            {
                try{
                    FileInfo fi = new FileInfo(file);
                    Interlocked.Add(ref bytesP, fi.Length);

                    //from above, adapted to parallel
                    if(Path.GetExtension(file) == ".png" || Path.GetExtension(file) == ".jpeg" || Path.GetExtension(file) == ".jpg" ){
                        // imgSize += f.Length;
                        // imgFilesCount += 1;
                        Interlocked.Add(ref imgBytesP, fi.Length);
                        Interlocked.Add(ref imgFilesP, 1);
                    }

                }catch (Exception e){
                    //Console.WriteLine($"Error in file processing: '{e}'");
                }
            });


            //recursive directory run in parallel
            Parallel.ForEach(directories, d => {
                try{
                    parallelCountFilesFolders(d);
                }catch (Exception e){
                    //Console.WriteLine($"Error in recursive subdir call: '{e}'");
                }

            });



        }
        catch (Exception e){
            //Console.WriteLine($"Parallel error detected at top level: '{e}'");
        }

    }

    static void parallel(string dir){
        double time;
        


        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        
        parallelCountFilesFolders(dir);


        //End of run
        stopwatch.Stop();
        time = stopwatch.Elapsed.TotalSeconds;


        Console.WriteLine();
        Console.WriteLine($"Parallel Calculated in: '{time}' seconds");
        Console.WriteLine($"'{foldersP}' folders, '{allFilesP}' files, '{bytesP}' bytes");
        Console.WriteLine($"'{imgFilesP}' image files, '{imgBytesP}' bytes");
        

    }

}







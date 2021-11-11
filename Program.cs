using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RBU_Unpacker
{
    class Program
    {
        // the "magic" for the glob file
        static byte[] globFilesBin = new byte[13] { 0x47, 0x6C, 0x6F, 0x62, 0x46, 0x69, 0x6C, 0x65, 0x73, 0x2E, 0x74, 0x78, 0x74 };

        static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                switch (args[0])
                {
                    case "rbu":
                        // this just splits things into chunks
                        if (File.Exists(args[2]))
                        {
                            var file = new FileStream(args[1], FileMode.Open);
                            var reader = new BinaryReader(file);

                            Console.WriteLine(reader.ReadInt32());

                            // Some unknown value - seems to correspond to the pointer of the first chunk in the file in some way
                            var unknown = reader.ReadUInt32();

                            // The number of chunks in the STR
                            var numChunks = reader.ReadUInt32() + 1;

                            // Read past empty bytes
                            reader.ReadBytes(0x34);

                            for (var i = 0; i < numChunks; i++)
                            {
                                var startPointer = reader.ReadInt32();
                                var chunkSize = reader.ReadInt32();
                                var endPointer = reader.ReadInt32();

                                var tempPosition = reader.BaseStream.Position;

                                // Advance base stream to pointer of first chunk
                                reader.BaseStream.Position = startPointer;

                                var bytes = reader.ReadBytes(chunkSize);
                                File.WriteAllBytes(args[2] + "/chunk_" + i, bytes);

                                // Set position to next chunk
                                reader.BaseStream.Position = tempPosition + 0x34;
                            }

                        }
                        else
                        {
                            Console.WriteLine("You must provide a file.");
                        }
                        break;
                    case "rbds":
                        switch (args[1])
                        {
                            case "unpack":
                                if (File.Exists(args[2]))
                                {
                                    Console.WriteLine("Extracting chunks from " + args[2]);
                                    extractRBDSChunks(args);

                                }
                                else
                                {
                                    Console.WriteLine("You must provide a file.");
                                }
                                break;
                            case "repack":
                                repackRBDSChunks(args);
                                break;
                        }
                        break;
                }
            }
            else
            {
                Console.WriteLine("You must provide a game and an input and output filename.\nExample: rbmobileunpacker rbds unpack Glob.bin outputFolder");
            }
        }

        static public void extractRBDSChunks(string[] args)
        {
            var file = new FileStream(args[2], FileMode.Open);
            var reader = new BinaryReader(file);

            // No file count so we stop reading pointers when the first chunk is hit
            var startChunkPointer = reader.ReadUInt32();
            var chunkCount = startChunkPointer / 0x4;

            Console.WriteLine(args[2] + " has " + chunkCount + " chunks");

            reader.BaseStream.Position = 0;

            var chunkBytes = new List<byte[]>();
            var fileNames = new List<string>();

            byte[] globFilesBytes = new byte[0];

            for (var i = 0; i < chunkCount - 1; i++)
            {
                var tempPosition = reader.BaseStream.Position;

                var startPointer = reader.ReadInt32();
                var endPointer = reader.ReadInt32();

                // Advance base stream to pointer of first chunk
                reader.BaseStream.Position = startPointer;

                var bytes = reader.ReadBytes(endPointer - startPointer);
                chunkBytes.Add(bytes);

                // Set position to next chunk
                reader.BaseStream.Position = tempPosition + 0x4;
            }

            foreach (var array in chunkBytes)
            {
                // do boyer moore search to find the glob files
                // i am not sure how the game itself knows which chunk is the glob file
                // unless i am dumb and am missing something, the brute force approach works but is NOT optimal
                if (BoyerMoore.IndexOf(array, globFilesBin) != -1)
                {
                    globFilesBytes = array;
                    Console.WriteLine("Found GlobFiles.txt, chunks will be named!");
                    break;
                }
            }

            if (globFilesBytes.Length != 0)
            {
                var numFileNames = globFilesBytes.Length / 0x40;
                var chunkReader = new BinaryReader(new MemoryStream(globFilesBytes));
                char[] charsToTrim = { ' ', '\n' };

                for (var i = 0; i < numFileNames; i++)
                {
                    var stringBytes = chunkReader.ReadBytes(0x40);
                    var trimmedString = System.Text.Encoding.Default.GetString(stringBytes).Trim(charsToTrim);
                    fileNames.Add(trimmedString);
                }
            }

            foreach (var array in chunkBytes.Select((value, i) => new { i, value }))
            {
                if (fileNames.Count != 0)
                {
                    Console.WriteLine("Writing " + fileNames[array.i] + " to " + args[3]);
                    File.WriteAllBytes(args[3] + "/" + fileNames[array.i], array.value);
                }
                else
                {
                    File.WriteAllBytes(args[3] + "/chunk_" + array.i, array.value);
                }
            }



        }

        static private void repackRBDSChunks(string[] args)
        {
            if (Directory.Exists(args[2]))
            {
                if (File.Exists(args[2] + "/GlobFiles.txt"))
                {
                    Console.WriteLine("GlobFiles.txt exists!");

                    var fileNames = new List<string>();
                    var chunkReader = new BinaryReader(new FileStream(args[2] + "/GlobFiles.txt", FileMode.Open));
                    var numFileNames = chunkReader.BaseStream.Length / 0x40;
                    char[] charsToTrim = { ' ', '\n' };

                    for (var i = 0; i < numFileNames; i++)
                    {
                        var stringBytes = chunkReader.ReadBytes(0x40);
                        var trimmedString = System.Text.Encoding.Default.GetString(stringBytes).Trim(charsToTrim);
                        fileNames.Add(trimmedString);
                    }

                    // Stupid workaround to fix chunk file at the end of the chunk table
                    fileNames.Add("nullfile");

                    chunkReader.Close();

                    var writer = new BinaryWriter(new FileStream(args[3], FileMode.Create));

                    // Write a blank header
                    writer.Write(new byte[fileNames.Count * 0x4]);

                    foreach (var name in fileNames.Select((value, i) => new { i, value }))
                    {
                        if (name.value != "nullfile")
                        {
                            byte[] bytes = System.IO.File.ReadAllBytes(args[2] + "/" + name.value);

                            writer.BaseStream.Position = 0 + (name.i * 0x4);
                            writer.Write((uint)writer.BaseStream.Length);

                            writer.BaseStream.Position = writer.BaseStream.Length;

                            writer.Write(bytes);
                        }
                        else
                        {
                            writer.BaseStream.Position = 0 + (name.i * 0x4);
                            writer.Write((uint)writer.BaseStream.Length);

                            writer.BaseStream.Position = writer.BaseStream.Length;
                        }
                    }

                    writer.Close();
                }
                else
                {
                    Console.WriteLine("The input directory is missing GlobFiles.txt");
                }
            }
        }
    }
}
